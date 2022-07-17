using System.Collections.Generic;
using UnityEngine;

namespace VoxelbasedCom
{
    public class VoxelBrush : MonoBehaviour
    {
        public Shape shape;
        public OperationType operationType;
        private Dictionary<Vector3, BaseModification> modifyVertex;

        public BaseModification AsModification()
        {
            return new BaseModification(transform.position, operationType, shape, transform.localScale.x);
        }

        public Vector3 GetCoordinates()
        {
            Vector3 pos = new Vector3();
            pos.x = Mathf.CeilToInt(transform.position.x);
            pos.y = Mathf.CeilToInt(transform.position.y);
            pos.z = Mathf.CeilToInt(transform.position.z);
            return pos;
        }
    }
}
