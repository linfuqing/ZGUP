namespace ZG
{
    public struct Point
    {
        public int x;
        public int y;

        public static Point operator *(Point x, int y)
        {
            return new Point(x.x * y, x.y * y);
        }

        public static Point operator +(Point x, Point y)
        {
            return new Point(x.x + y.x, x.y + y.y);
        }

        public static Point operator -(Point x, Point y)
        {
            return new Point(x.x - y.x, x.y - y.y);
        }

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
