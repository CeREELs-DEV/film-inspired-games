using UnityEngine;
using UnityEngine.EventSystems;

namespace FilmInspiredGames.Burning
{
    public sealed class BurningAct1InputForwarder : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private BurningAct1FlowController flow;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
            {
                flow?.HandleScreenClick();
            }
        }
    }
}
