using System;
using System.Collections.Generic;
using System.Text;

namespace Smartcrop
{
    public static class RatioUtil
    {
        public static Rectangle ComputeRatio(int originWidth, int originHeight, string ratio, int maxWidth = 0)
        {
            var parts = ratio.Split(':');
            int w = int.Parse(parts[0]);
            int h = int.Parse(parts[1]);

            double numRation = (double)w / h;

            var result = compute(originWidth, originHeight, numRation);
            if (maxWidth > 0)  // if specified
            {
                if (result.width > maxWidth)
                {
                    return new Rectangle(0, 0, maxWidth, (int)(maxWidth / numRation));
                }
            }

            return new Rectangle(0, 0, result.width, result.height);

            static (int width, int height) compute(int originWidth, int originHeight, double ratio)
            {
                double originRatio = (double)originWidth / originHeight;
                if (originRatio <= ratio)
                {
                    return (originWidth, (int)(originWidth / ratio));
                }

                return ((int)(originHeight * ratio), originHeight);
            }
        }
    }
}
