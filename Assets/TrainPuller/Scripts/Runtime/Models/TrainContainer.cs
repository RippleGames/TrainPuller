using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainContainer : MonoBehaviour
    {
        [FormerlySerializedAs("cardSlot")] [SerializeField]
        private List<CardSlot> cardSlots = new List<CardSlot>();

        [SerializeField] private List<CardSlot> fullCarSlots = new List<CardSlot>();
        public TrainMovement trainMovement;
        public bool isAllFull;

        public void SetCartSlots(List<CartScript> carts)
        {
            foreach (var cart in trainMovement.carts)
            {
                cardSlots.AddRange(cart.GetCardSlots());
            }
        }


        private CardSlot GetClosestSlot(Transform cardTransform)
        {
            if (!cardTransform) return null;
            if (cardSlots.Count > 0)
            {
                var nearestSlot = cardSlots.OrderBy(obj => (cardTransform.position - obj.cartSlotTransform.position).sqrMagnitude).FirstOrDefault();
              

                fullCarSlots.Add(nearestSlot);
                cardSlots.Remove(nearestSlot);
                if (cardSlots.Count <= 0)
                {
                    isAllFull = true;
                }
                return nearestSlot;
            }

            isAllFull = true;
            return null;
        }

        public void TakeCard(CardScript takenCard)
        {
            if (isAllFull) return;
            var emptySlot = GetClosestSlot(takenCard.gameObject.transform);
            if (emptySlot == null) return;
            if (takenCard == null) return;
            emptySlot.isEmpty = false;
            takenCard.transform.SetParent(emptySlot.cartSlotTransform);
            var midPoint = (takenCard.transform.localPosition + emptySlot.cartSlotTransform.localPosition) / 2;
            midPoint.y += 1f;
            var path = new[] { takenCard.transform.localPosition, midPoint, Vector3.zero };

            takenCard.transform.DOLocalPath(path, 0.5f, PathType.CatmullRom)
                .SetEase(Ease.InSine).OnComplete(() =>
                {
                    var oldScale = takenCard.transform.localScale;
                    takenCard.transform.DOScale(oldScale * 1.1f, 0.15f).OnComplete(() =>
                    {
                        takenCard.transform.DOScale(oldScale, 0.15f);

                    });

                });
            takenCard.transform.DOLocalRotate(new Vector3(0f, 90f, 90f), 0.75f).SetEase(Ease.InSine);
        }
    }
}

[System.Serializable]
public class CardSlot
{
    public Transform cartSlotTransform;
    public bool isEmpty = true;
}