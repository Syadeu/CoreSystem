namespace Syadeu.Database
{
    public enum Direction
    {
        NONE = 0,

        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,

        UpDown = Up | Down,
        UpLeft = Up | Left,
        UpRight = Up | Right,
        DownLeft = Up | Left,
        DownRight = Up | Right,

        LeftRight = Left | Right,

        UpLeftDown = Up | Left | Down,
        UpRightDown = Up | Right | Down,
        LeftUpRight = Left | Up | Right,
        LeftDownRight = Left | Down | Right,

        UpRightCorner = Up | Right,
        UpLeftCorner = Up | Left,
        DownRightCorner = Down | Right,
        DownLeftCorner = Down | Left,

        UpDownLeftRight = ~0
    }
}
