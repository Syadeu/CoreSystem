namespace Syadeu.Database
{
    /// <summary>
    /// Value의 값이 바뀌면 OnValueChange event를 호출하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObClass<T> where T : class
    {
        public delegate void ValueChangeAction(T current, T target);

        public ObValueDetection DetectionFlag { get; }

        private T m_Value;
        public T Value
        {
            get { return m_Value; }
            set
            {
                T temp = m_Value;
                m_Value = value;

                if (DetectionFlag == ObValueDetection.Constant)
                {
                    OnValueChange?.Invoke(temp, value);
                }
                else if (DetectionFlag == ObValueDetection.Changed)
                {
                    if (!temp.Equals(value)) OnValueChange?.Invoke(temp, value);
                }
            }
        }

        public event ValueChangeAction OnValueChange;
        public ObClass(ObValueDetection flag)
        {
            DetectionFlag = flag;
        }
    }
}
