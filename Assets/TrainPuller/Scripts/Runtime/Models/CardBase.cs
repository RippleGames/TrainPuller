using System.Collections.Generic;
using TemplateProject.Scripts.Data;
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

        public CardScript TryGetCardFromStack(LevelData.GridColorType trainColor, TrainContainer trainContainer)
        {
            if (cardStack.Count <= 0) return null;
            if (cardStack[^1].GetCardColor() != trainColor) return null;
            var card = cardStack[^1];
            if (trainContainer.GetComingCards().Contains(card) || trainContainer.GetComingCards().Count > trainContainer.GetFullCardSlots().Count) return null;
            cardStack.RemoveAt(cardStack.Count - 1);
            return card;
        }
    }
}