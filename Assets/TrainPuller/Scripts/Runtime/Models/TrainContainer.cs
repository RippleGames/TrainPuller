using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainContainer : MonoBehaviour
    {
        [SerializeField] private List<CardSlot> cardSlots = new List<CardSlot>();

        [SerializeField] private List<CardSlot> fullCarSlots = new List<CardSlot>();
        public TrainMovement trainMovement;
        public bool isAllFull;
        private Queue<CardScript> _cardQueue = new Queue<CardScript>();
        [SerializeField] private List<CardScript> takenCards;
        private Coroutine _cardTakeCoroutine;

        public void SetCartSlots(List<CartScript> carts)
        {
            for (var i = 0; i < carts.Count; i++)
            {
                var cart = carts[i];

                if (i != 0)
                {
                    cart.GetCardSlots().Reverse();
                }

                cardSlots.AddRange(cart.GetCardSlots());
            }
            // InverseSlotList();
        }

        public void InverseSlotList()
        {
            cardSlots.Reverse();
        }

        private CardSlot GetClosestSlot(Transform cardTransform)
        {
            if (!cardTransform) return null;
            if (cardSlots.Count > 0)
            {
                var nearestSlot = cardSlots.FirstOrDefault(x => x.isEmpty);


                fullCarSlots.Add(nearestSlot);
                cardSlots.Remove(nearestSlot);
                if (cardSlots.Count <= 0)
                {
                    isAllFull = true;
                    DOVirtual.DelayedCall(_cardQueue.Count * 0.4f, () =>
                    {
                        trainMovement.TryBlastConfetti();
                        DOVirtual.DelayedCall(trainMovement.carts.Count * 0.2f,
                            () => { trainMovement.TryDoScaleEffect(); });
                    });
                }

                return nearestSlot;
            }

            isAllFull = true;
            return null;
        }

        public void TakeCard(CardScript takenCard)
        {
            if (isAllFull) return;
            var emptySlot = GetClosestSlot(takenCard.transform);
            if (emptySlot == null) return;
            if (!takenCard) return;

            emptySlot.isEmpty = false;
            StartCoroutine(MoveCardToSlot(takenCard, emptySlot));
        }

        private IEnumerator MoveCardToSlot(CardScript takenCard, CardSlot targetSlot)
        {
            var cardTransform = takenCard.transform;
            var slotTransform = targetSlot.cartSlotTransform;

            var startPos = cardTransform.position;
            var endPos = slotTransform.position;
            var midPoint = (startPos + endPos) / 2;
            midPoint.y += 2f;

            var worldTargetRotation = slotTransform.rotation * Quaternion.Euler(0f, 90f, 90f);

            var duration = 0.35f;
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / duration;

                var updatedSlotPosition = slotTransform.position;
                var currentTarget = Vector3.Lerp(Vector3.Lerp(startPos, midPoint, t), updatedSlotPosition, t);
                cardTransform.position = currentTarget;

                cardTransform.rotation = Quaternion.Slerp(cardTransform.rotation, worldTargetRotation, t);

                yield return null;
            }

            takenCard.SetOutlineColor(trainMovement.GetOutlineColor());
            if (!takenCards.Contains(takenCard))
            {
                takenCards.Add(takenCard);
            }

            var oldScale = cardTransform.localScale;
            cardTransform.DOScale(oldScale * 1.2f, 0.1f).OnComplete(() => { cardTransform.DOScale(oldScale, 0.1f); });
            cardTransform.SetParent(slotTransform);
            cardTransform.localRotation = Quaternion.Euler(0f, 90f, 90f);
            cardTransform.localPosition = Vector3.zero;
        }

        public void TakeCardWithDelay(CardScript takenCard)
        {
            if (_cardQueue.Contains(takenCard)) return;
            _cardQueue.Enqueue(takenCard);
            if (_cardQueue.Count == 1 && _cardTakeCoroutine == null)
            {
                _cardTakeCoroutine = StartCoroutine(ProcessCardQueue());
            }
        }

        private IEnumerator ProcessCardQueue()
        {
            while (_cardQueue.Count > 0)
            {
                var card = _cardQueue.Dequeue();
                TakeCard(card);
                yield return new WaitForSeconds(0.05f);
            }

            _cardTakeCoroutine = null;
        }

        public List<CardScript> GetTakenCards()
        {
            return takenCards;
        }
    }
}

[System.Serializable]
public class CardSlot
{
    public Transform cartSlotTransform;
    public bool isEmpty = true;
}