using System;

namespace Syadeu.Presentation.Map
{
    [Obsolete]
    public struct GridPath16
    {
        public enum Result
        {
            Failed,
            Success
        }

        public GridPathTile a, b, c, d;
        public GridPathTile e, f, g, h;
        public GridPathTile i, j, k, l;
        public GridPathTile m, n, o, p;

        private int m_PathLength;

        public GridPathTile this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;
                    case 3: return d;
                    case 4: return e;
                    case 5: return f;
                    case 6: return g;
                    case 7: return h;
                    case 8: return i;
                    case 9: return j;
                    case 10: return k;
                    case 11: return l;
                    case 12: return m;
                    case 13: return n;
                    case 14: return o;
                    case 15: return p;
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (index)
                {
                    case 0: { a = value; return; };
                    case 1: { b = value; return; };
                    case 2: { c = value; return; };
                    case 3: { d = value; return; };
                    case 4: { e = value; return; };
                    case 5: { f = value; return; };
                    case 6: { g = value; return; };
                    case 7: { h = value; return; };
                    case 8: { i = value; return; };
                    case 9: { j = value; return; };
                    case 10: { k = value; return; };
                    case 11: { l = value; return; };
                    case 12: { m = value; return; };
                    case 13: { n = value; return; };
                    case 14: { o = value; return; };
                    case 15: { p = value; return; };
                }

                throw new IndexOutOfRangeException();
            }
        }
        public int PathLength => m_PathLength;

        public Result result;

        public void Initialize(int pathLength)
        {
            for (int i = 0; i < 16; i++)
            {
                this[i] = GridPathTile.Empty;
            }

            m_PathLength = pathLength;
        }
    }
}
