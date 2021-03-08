using System.Collections;

using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Syadeu.ECS
{
    public class ECSPathAgentModule : ECSModule
    {
        private Vector3[] m_Path = null;

        private int ID { get; }
        private int AgentTypeID { get; }
        private Transform Transform { get; }

        public Vector3 TargetPosition { get; private set; }
        public int TargetAreaMask { get; private set; }
        public Vector3[] Path => m_Path;

        public bool IsMoving { get; private set; } = false;
        public bool IsPause { get; private set; } = false;
        private Coroutine Update { get; set; } = null;

        public float Speed { get; set; } = 2;

        public ECSPathAgentModule(Transform tr, int agentTypeID, float maxTravelDistance = -1, float exitPathNodeSize = -1f, float radius = 1)
        {
            Transform = tr;
            AgentTypeID = agentTypeID;
            ID = ECSPathAgentSystem.RegisterPathfinder(tr, agentTypeID, maxTravelDistance, exitPathNodeSize, radius);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (Update != null)
            {
                CoreSystem.Instance.StopCoroutine(Update);
            }
        }

        public void Move(Vector3 direction, int areaMask = -1)
        {
            if (ECSPathAgentSystem.Raycast(out _, AgentTypeID, CoreSystem.GetPosition(Transform), direction, areaMask))
            {
                return;
            }
        }
        public void MoveTo(Vector3 target, int areaMask = -1)
        {
            if (!IsMoving)
            {
                Update = CoreSystem.Instance.StartCoroutine(Updater());
            }

            TargetAreaMask = areaMask;
            TargetPosition = target;
            IsMoving = true;

            ECSPathAgentSystem.SchedulePath(ID, target, areaMask);
        }

        private bool IsArrived(Vector3 current, Vector3 pos)
        {
            return
                current.x - ECSSettings.Instance.m_AgentNodeOffset <= pos.x &&
                current.x + ECSSettings.Instance.m_AgentNodeOffset >= pos.x &&

                current.y - ECSSettings.Instance.m_AgentNodeOffset <= pos.y &&
                current.y + ECSSettings.Instance.m_AgentNodeOffset >= pos.y &&

                current.z - ECSSettings.Instance.m_AgentNodeOffset <= pos.z &&
                current.z + ECSSettings.Instance.m_AgentNodeOffset >= pos.z;
        }

        private IEnumerator Updater()
        {
            yield return new WaitUntil(() => ECSPathAgentSystem.TryGetPathPositions(ID, out m_Path));
            //"start".ToLog();
            while (true)
            {
                //"in".ToLog();
                if (IsMoving && !IsPause)
                {
                    ECSPathAgentSystem.SchedulePath(ID, TargetPosition, TargetAreaMask);

                    if (ECSPathAgentSystem.TryGetPathPositions(ID, out m_Path))
                    {
                        Vector3 current = Transform.position;
                        Vector3 nextPos;

                        if (IsArrived(current, m_Path[1]) &&
                                m_Path.Length >= 3)
                        {
                            nextPos = m_Path[2];
                        }
                        else
                        {
                            nextPos = m_Path[1];
                        }

                        //$"{nextPos}".ToLog();
                        Vector3 dir = nextPos - current;

                        Vector3 pos = current + (dir.normalized * Time.deltaTime * Speed);
                        pos = ECSPathQuerySystem.ToLocation(pos, AgentTypeID).position;

                        //$"{pos} :: {paths[0]} :: {paths[1]}".ToLog();
                        //Transform.position = pos;
                        ECSPathAgentSystem.SetPosition(ID, pos);
                    }
                    else break;
                }

                yield return null;
            }

            m_Path = null;
            IsMoving = false;
            IsPause = false;
        }
    }
}
