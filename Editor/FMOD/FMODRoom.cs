using System.Collections.Generic;

using UnityEngine;

namespace SyadeuEditor
{
#if CORESYSTEM_FMOD
    using Syadeu.FMOD;
    public class FMODRoom : MonoBehaviour
    {
        public static Transform roomFolder;
        public static List<FMODRoom> insRooms = new List<FMODRoom>();

        public Bounds bounds;
        public FMODRoomVertice[] m_Vertices = new FMODRoomVertice[6];

        public bool drawBounds = false;

        public int index;
        public int backgroundType;
        [Range(0, 1)] public float directOcclusion;

        public static FMODRoom Set(SoundRoom room)
        {
            if (roomFolder == null)
            {
                var tempfolder = GameObject.Find("Syadeu.Extension.RoomEditor");
                if (tempfolder != null)
                {
                    roomFolder = tempfolder.transform;
                }
                else
                {
                    roomFolder = new GameObject("Syadeu.Extension.RoomEditor").transform;
                }

                //roomFolder.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
            var gameObj = new GameObject($"Syadeu.Extension.TempRoom_{insRooms.Count}");
            gameObj.transform.SetParent(roomFolder);
            var m_Instance = gameObj.AddComponent<FMODRoom>();

            for (int i = 0; i < m_Instance.m_Vertices.Length; i++)
            {
                m_Instance.m_Vertices[i] = new GameObject($"Syadeu.Extension.TempRoom.Vertice{i}").AddComponent<FMODRoomVertice>();
                m_Instance.m_Vertices[i].room = m_Instance;
                m_Instance.m_Vertices[i].transform.SetParent(gameObj.transform);
            }

            m_Instance.transform.position = room.Position;

            m_Instance.index = room.Index;
            m_Instance.backgroundType = room.BackgroundType;
            m_Instance.directOcclusion = room.Direct;

            m_Instance.bounds = new Bounds
            {
                center = m_Instance.transform.position,
                size = Vector3.Scale(m_Instance.transform.lossyScale, m_Instance.transform.localScale)
            };

            Vector3 center = m_Instance.bounds.center;
            // bottom
            m_Instance.m_Vertices[0].transform.localPosition = new Vector3(center.x, room.m_Vertices[0] != Vector3.zero ? room.m_Vertices[0].y : m_Instance.bounds.min.y, center.z);
            // left
            m_Instance.m_Vertices[1].transform.localPosition = new Vector3(room.m_Vertices[1] != Vector3.zero ? room.m_Vertices[1].x : m_Instance.bounds.min.x, center.y, center.z);
            // right
            m_Instance.m_Vertices[2].transform.localPosition = new Vector3(room.m_Vertices[2] != Vector3.zero ? room.m_Vertices[2].x : m_Instance.bounds.max.x, center.y, center.z);
            //forward
            m_Instance.m_Vertices[3].transform.localPosition = new Vector3(center.x, center.y, room.m_Vertices[3] != Vector3.zero ? room.m_Vertices[3].z : m_Instance.bounds.max.z);
            // backward
            m_Instance.m_Vertices[4].transform.localPosition = new Vector3(center.x, center.y, room.m_Vertices[4] != Vector3.zero ? room.m_Vertices[4].z : m_Instance.bounds.min.z);
            // upward
            m_Instance.m_Vertices[5].transform.localPosition = new Vector3(center.x, room.m_Vertices[5] != Vector3.zero ? room.m_Vertices[5].y : m_Instance.bounds.max.y, center.z);

            m_Instance.bounds.Encapsulate(m_Instance.m_Vertices[0].transform.position);
            m_Instance.bounds.Encapsulate(m_Instance.m_Vertices[1].transform.position);
            m_Instance.bounds.Encapsulate(m_Instance.m_Vertices[2].transform.position);
            m_Instance.bounds.Encapsulate(m_Instance.m_Vertices[3].transform.position);
            m_Instance.bounds.Encapsulate(m_Instance.m_Vertices[4].transform.position);
            m_Instance.bounds.Encapsulate(m_Instance.m_Vertices[5].transform.position);

            insRooms.Add(m_Instance);
            return m_Instance;
        }
    }
#endif
}
