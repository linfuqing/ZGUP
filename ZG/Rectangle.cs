using System;

namespace ZG
{
    public struct Rectangle
    {
        public Point min;
        public Point max;
        
        public static Rectangle operator +(Rectangle rectangle, Point point)
        {
            rectangle.min += point;
            rectangle.max += point;

            return rectangle;
        }

        public Rectangle(int minX, int minY, int maxX, int maxY)
        {
            min.x = minX;
            min.y = minY;
            max.x = maxX;
            max.y = maxY;
        }

        public bool IsContain(int minX, int minY, int maxX, int maxY)
        {
            return min.x <= minX &&
                min.y <= minY &&
                max.x >= maxX &&
                max.y >= maxY;
        }

        public bool IsContain(Rectangle rectangle)
        {
            return IsContain(rectangle.min.x, rectangle.min.y, rectangle.max.x, rectangle.max.y);
        }

        public bool IsIntersect(int minX, int minY, int maxX, int maxY)
        {
            return Math.Abs(min.x + max.x - minX - maxX) <=
                (max.x + maxX - min.x - minX) &&
                Math.Abs(min.y + max.y - minY - maxY) <=
                (max.y + maxY - min.y - minY);
        }

        public bool IsIntersect(Rectangle rectangle)
        {
            return IsIntersect(rectangle.min.x, rectangle.min.y, rectangle.max.x, rectangle.max.y);
        }

        public void ToPoint(int x, int y, out int pointX, out int pointY, out int distanceX, out int distanceY)
        {
            int currentLength, currentLengthX, currentLengthY;
            bool isOverX = x >= min.x && x <= max.x, isOverY = y >= min.y && y <= max.y;
            if (isOverX || isOverY)
            {
                if (isOverX && isOverY)
                {
                    pointX = x;
                    pointY = y;

                    distanceX = 0;
                    distanceY = 0;

                    return;
                }
                else if (isOverX)
                {
                    distanceX = 0;

                    currentLength = Math.Abs(y - min.y);
                    currentLengthY = Math.Abs(y - max.y);
                    
                    if (currentLengthY < currentLength)
                    {
                        currentLength = currentLengthY;

                        distanceY = max.y;
                    }
                    else
                        distanceY = min.y;

                    pointX = x;
                    pointY = distanceY;
                    
                    return;
                }
                else if (isOverY)
                {
                    distanceY = 0;

                    currentLength = Math.Abs(x - min.x);
                    currentLengthX = Math.Abs(x - max.x);

                    if (currentLengthX < currentLength)
                    {
                        currentLength = currentLengthX;

                        distanceX = max.x;
                    }
                    else
                        distanceX = min.x;

                    pointX = distanceX;
                    pointY = y;
                    
                    return;
                }
            }

            currentLengthX = Math.Abs(x - min.x);
            currentLength = Math.Abs(x - max.x);

            if (currentLength < currentLengthX)
            {
                currentLengthX = currentLength;

                distanceX = max.x;
            }
            else
                distanceX = min.x;

            currentLengthY = Math.Abs(y - min.y);
            currentLength = Math.Abs(y - max.y);

            if (currentLength < currentLengthY)
            {
                currentLengthY = currentLength;

                distanceY = max.y;
            }
            else
                distanceY = min.y;

            pointX = distanceX;
            pointY = distanceY;
        }
    }
}
