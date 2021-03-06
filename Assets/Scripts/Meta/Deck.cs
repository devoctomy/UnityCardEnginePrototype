﻿using Assets.Scripts.Debugging;
using Assets.Scripts.Extensions;
using Assets.Scripts.Meta.Extensions;
using Assets.Scripts.Utility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Meta
{

    public class Deck
    {

        #region private objects

        private System.Random cRndRandom;
        private Boolean cBlnInitialised;
        private Texture2D cTexBackTexture;

        #endregion

        #region public properties

        [JsonProperty(Required = Required.Always)]
        public DeckInfo Info;

        [JsonProperty(Required = Required.Always)]
        public DeckCard[] Cards;

        [JsonIgnore]
        public List<DeckCard> Stack;

        [JsonIgnore]
        public CardManager Manager { get; private set; }

        [JsonIgnore]
        public static Dictionary<GameObject, DeckCard> CardsByGameObject = new Dictionary<GameObject, DeckCard>();

        public Texture2D CardBackTexture
        {
            get
            {
                if(cTexBackTexture == null)
                {
                    cTexBackTexture = IOUtility.LoadStreamingAssestsFileAsTexture2D(String.Format("Cards/{0}", Info.BackImageFile));
                }
                return (cTexBackTexture);
            }
        }

        #endregion

        #region constructor / destructor

        public Deck()
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.Constructor()");

            cRndRandom = new System.Random(Environment.TickCount);
            Stack = new List<DeckCard>();
        }

        #endregion

        #region public methods

        public static Deck LoadFromAssets(CardManager iManager,
            String iName)
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.LoadFromAssets({0},'{1}').", iManager.GetHashCode(), iName);
            Logman.Log(BaseLogger.MessageType.Information, "Loading deck file '{0}'.", iName);

            String pStrConfigJSON = IOUtility.LoadStreamingAssestsFileAsString(String.Format("Cards/{0}.deck", iName));

            Logman.Log(BaseLogger.MessageType.Information, "Parsing deck file data.");
            Deck pDekDeck = JsonConvert.DeserializeObject<Deck>(pStrConfigJSON);
            Logman.Log(BaseLogger.MessageType.Success, "Successfully parsed deck file data.");

            pDekDeck.Initialise(iManager);
            return (pDekDeck);
        }

        public void Initialise(CardManager iManager)
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.Initialise({0}).", iManager.GetHashCode());

            if (!cBlnInitialised)
            {
                Manager = iManager;
                foreach (DeckCard curCard in Cards)
                {
                    curCard.Initialise(this, Info.BackImageFile);
                    Stack.Add(curCard);
                }
                ShuffleStack(Stack.Count * Stack.Count);
                cBlnInitialised = true;
            }
        }

        public void ShuffleStack(Int32 iCount = 500)
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.ShuffleStack({0}).", iCount);

            List<DeckCard> pLisStack = Stack.ToList();
            for(Int32 curShuffle = 0; curShuffle < iCount; curShuffle++)
            {
                Int32 pIntRandomIndex = cRndRandom.Next(0, pLisStack.Count);
                DeckCard pDCdShuffleCard = pLisStack[pIntRandomIndex];
                pLisStack.RemoveAt(pIntRandomIndex);
                Int32 pIntRandomInsertIndex = cRndRandom.Next(0, pLisStack.Count);
                pLisStack.Insert(pIntRandomInsertIndex, pDCdShuffleCard);
            }
            Stack = pLisStack;
        }

        public void CreateStack(GameObject iCardPrefab,
            Vector2 iPosition,
            DeckCard.CardFacing iFacing = DeckCard.CardFacing.Down)
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.CreateStack({0}, {1}, {2}).", iCardPrefab.name, iPosition.ToString(), iFacing);

            Vector3 pVecCurPosition = new Vector3(iPosition.x, 0, iPosition.y);
            for (Int32 curCard = 0; curCard < Stack.Count; curCard++)
            {
                CreateCard(Stack[curCard], iCardPrefab, pVecCurPosition, iFacing);
                pVecCurPosition = new Vector3(pVecCurPosition.x, pVecCurPosition.y + Stack[curCard].Thickness + 0.03f, pVecCurPosition.z);
            }
        }

        public void CreateCard(GameObject iCardPrefab,
            Vector3 iPosition,
            DeckCard.CardFacing iFacing = DeckCard.CardFacing.Down,
            params DeckCardTag[] iTags)
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.CreateCard({0}, {1}, {2}, {3}).", iCardPrefab.name, iPosition.ToString(), iFacing, iTags.ToTagString());

            List<DeckCard> pLisCards = GetCards(iTags);
            pLisCards[0].Create(iCardPrefab, iPosition, iFacing);
            CardsByGameObject.Add(pLisCards[0].GameObjectRef, pLisCards[0]);
        }

        public void CreateCard(DeckCard iCard,
            GameObject iCardPrefab,
            Vector3 iPosition,
            DeckCard.CardFacing iFacing = DeckCard.CardFacing.Down)
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.CreateCard({0}, {1}, {2}).", iCardPrefab.name, iPosition.ToString(), iFacing);

            iCard.Create(iCardPrefab, iPosition, iFacing);
            CardsByGameObject.Add(iCard.GameObjectRef, iCard);
        }

        public List<DeckCard> GetCards(params DeckCardTag[] iTags)
        {
            Logman.Log(BaseLogger.MessageType.Verbose, "Deck.GetCards({0}).", iTags.ToTagString());

            List<DeckCard> pLisCards = new List<DeckCard>();
            foreach(DeckCard curCard in Cards)
            {
                Int32 pIntMatches = 0;
                foreach(DeckCardTag curTag in iTags)
                {
                    if(curCard.TagsByName.ContainsKey(curTag.Name) && 
                        (curCard.TagsByName[curTag.Name].Value == curTag.Value))
                    {
                        pIntMatches += 1;
                    }
                }
                Boolean pBlnMatch = (pIntMatches == iTags.Length);
                if (pBlnMatch)
                {
                    pLisCards.Add(curCard);
                }
            }
            return (pLisCards);
        }

        #endregion

    }

}