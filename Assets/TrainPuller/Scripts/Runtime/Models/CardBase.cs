using System.Collections.Generic;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class CardBase : MonoBehaviour
    {
        [SerializeField] private List<CardScript> cardStack = new List<CardScript>();

        public void AddToCardStack(CardScript card)
        {
            if (!cardStack.Contains(card))
            {
                cardStack.Add(card);
            }
        }

        public CardScript GetCardFromStack()
        {
            if (cardStack.Count <= 0) return null;
            var card = cardStack[^1];
            cardStack.RemoveAt(cardStack.Count-1);
            return card;

        }
    }
}
