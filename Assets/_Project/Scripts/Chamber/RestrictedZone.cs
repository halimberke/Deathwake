using UnityEngine;

namespace Game.Chamber
{
    // Yasak bölge. Taret gibi sistemler "bu nokta bölgenin içinde mi?" diye sorar.
    // Bölgenin şekli bir trigger BoxCollider'dır; oyuncuyu engellemez.
    [RequireComponent(typeof(BoxCollider))]
    public class RestrictedZone : MonoBehaviour
    {
        private BoxCollider area;

        private void Awake()
        {
            area = GetComponent<BoxCollider>();
            area.isTrigger = true;
        }

        // ClosestPoint, nokta kutunun içindeyse noktanın kendisini döndürür.
        // Oda döndürülmüş olsa bile doğru çalışır (eksen hizalı sınır kutusunun aksine).
        public bool Contains(Vector3 point)
        {
            return (area.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
        }
    }
}
