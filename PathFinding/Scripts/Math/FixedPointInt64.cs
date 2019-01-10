﻿using System;
using System.IO;
using UnityEngine;

namespace BlueNoah.Math.FixedPoint {

    [Serializable]
    public partial struct FixedPointInt64 : IEquatable<FixedPointInt64>, IComparable<FixedPointInt64> {

        [SerializeField]
        public long _serializedValue;

		public const long MAX_VALUE = long.MaxValue;
		public const long MIN_VALUE = long.MinValue;
		public const int NUM_BITS = 64;
		public const int FRACTIONAL_PLACES = 32;
		public const long ONE = 1L << FRACTIONAL_PLACES;
		public const long TEN = 10L << FRACTIONAL_PLACES;
		public const long HALF = 1L << (FRACTIONAL_PLACES - 1);
		public const long PI_TIMES_2 = 0x6487ED511;
		public const long PI = 0x3243F6A88;
		public const long PI_OVER_2 = 0x1921FB544;
		public const int LUT_SIZE = (int)(PI_OVER_2 >> 15);

        // Precision of this type is 2^-32, that is 2,3283064365386962890625E-10
        public static readonly decimal Precision = (decimal)(new FixedPointInt64(1L));//0.00000000023283064365386962890625m;
        public static readonly FixedPointInt64 MaxValue = new FixedPointInt64(MAX_VALUE-1);
        public static readonly FixedPointInt64 MinValue = new FixedPointInt64(MIN_VALUE+2);
        public static readonly FixedPointInt64 One = new FixedPointInt64(ONE);
		public static readonly FixedPointInt64 Ten = new FixedPointInt64(TEN);
        public static readonly FixedPointInt64 Half = new FixedPointInt64(HALF);

        public static readonly FixedPointInt64 Zero = new FixedPointInt64();
        public static readonly FixedPointInt64 PositiveInfinity = new FixedPointInt64(MAX_VALUE);
        public static readonly FixedPointInt64 NegativeInfinity = new FixedPointInt64(MIN_VALUE+1);
        public static readonly FixedPointInt64 NaN = new FixedPointInt64(MIN_VALUE);

        public static readonly FixedPointInt64 EN1 = FixedPointInt64.One / 10;
        public static readonly FixedPointInt64 EN2 = FixedPointInt64.One / 100;
        public static readonly FixedPointInt64 EN3 = FixedPointInt64.One / 1000;
        public static readonly FixedPointInt64 EN4 = FixedPointInt64.One / 10000;
        public static readonly FixedPointInt64 EN5 = FixedPointInt64.One / 100000;
        public static readonly FixedPointInt64 EN6 = FixedPointInt64.One / 1000000;
        public static readonly FixedPointInt64 EN7 = FixedPointInt64.One / 10000000;
        public static readonly FixedPointInt64 EN8 = FixedPointInt64.One / 100000000;
        public static readonly FixedPointInt64 Epsilon = FixedPointInt64.EN3;

        /// <summary>
        /// The value of Pi
        /// </summary>
        public static readonly FixedPointInt64 Pi = new FixedPointInt64(PI);
        public static readonly FixedPointInt64 PiOver2 = new FixedPointInt64(PI_OVER_2);
        public static readonly FixedPointInt64 PiTimes2 = new FixedPointInt64(PI_TIMES_2);
        public static readonly FixedPointInt64 PiInv = (FixedPointInt64)0.3183098861837906715377675267M;
        public static readonly FixedPointInt64 PiOver2Inv = (FixedPointInt64)0.6366197723675813430755350535M;

        public static readonly FixedPointInt64 Deg2Rad = Pi / new FixedPointInt64(180);

        public static readonly FixedPointInt64 Rad2Deg = new FixedPointInt64(180) / Pi;

		public static readonly FixedPointInt64 LutInterval = (FixedPointInt64)(LUT_SIZE - 1) / PiOver2;

        public static int Sign(FixedPointInt64 value) {
            return
                value._serializedValue < 0 ? -1 :
                value._serializedValue > 0 ? 1 :
                0;
        }

        public static FixedPointInt64 Abs(FixedPointInt64 value) {
            if (value._serializedValue == MIN_VALUE) {
                return MaxValue;
            }
            // branchless implementation, see http://www.strchr.com/optimized_abs_function
            var mask = value._serializedValue >> 63;
            return new FixedPointInt64((value._serializedValue + mask) ^ mask);
        }

        /// <summary>
        /// Returns the absolute value of a Fix64 number.
        /// FastAbs(Fix64.MinValue) is undefined.
        /// </summary>
        public static FixedPointInt64 FastAbs(FixedPointInt64 value) {
            // branchless implementation, see http://www.strchr.com/optimized_abs_function
            var mask = value._serializedValue >> 63;
            return new FixedPointInt64((value._serializedValue + mask) ^ mask);
        }


        /// <summary>
        /// Returns the largest integer less than or equal to the specified number.
        /// </summary>
        public static FixedPointInt64 Floor(FixedPointInt64 value) {
            // Just zero out the fractional part
            return new FixedPointInt64((long)((ulong)value._serializedValue & 0xFFFFFFFF00000000));
        }

        /// <summary>
        /// Returns the smallest integral value that is greater than or equal to the specified number.
        /// </summary>
        public static FixedPointInt64 Ceiling(FixedPointInt64 value) {
            var hasFractionalPart = (value._serializedValue & 0x00000000FFFFFFFF) != 0;
            return hasFractionalPart ? Floor(value) + One : value;
        }

        /// <summary>
        /// Rounds a value to the nearest integral value.
        /// If the value is halfway between an even and an uneven value, returns the even value.
        /// </summary>
        public static FixedPointInt64 Round(FixedPointInt64 value) {
            var fractionalPart = value._serializedValue & 0x00000000FFFFFFFF;
            var integralPart = Floor(value);
            if (fractionalPart < 0x80000000) {
                return integralPart;
            }
            if (fractionalPart > 0x80000000) {
                return integralPart + One;
            }
            // if number is halfway between two values, round to the nearest even number
            // this is the method used by System.Math.Round().
            return (integralPart._serializedValue & ONE) == 0
                       ? integralPart
                       : integralPart + One;
        }

        /// <summary>
        /// Adds x and y. Performs saturating addition, i.e. in case of overflow, 
        /// rounds to MinValue or MaxValue depending on sign of operands.
        /// </summary>
        public static FixedPointInt64 operator +(FixedPointInt64 x, FixedPointInt64 y) {
            return new FixedPointInt64(x._serializedValue + y._serializedValue);
        }

        /// <summary>
        /// Adds x and y performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static FixedPointInt64 OverflowAdd(FixedPointInt64 x, FixedPointInt64 y) {
            var xl = x._serializedValue;
            var yl = y._serializedValue;
            var sum = xl + yl;
            // if signs of operands are equal and signs of sum and x are different
            if (((~(xl ^ yl) & (xl ^ sum)) & MIN_VALUE) != 0) {
                sum = xl > 0 ? MAX_VALUE : MIN_VALUE;
            }
            return new FixedPointInt64(sum);
        }

        /// <summary>
        /// Adds x and y witout performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static FixedPointInt64 FastAdd(FixedPointInt64 x, FixedPointInt64 y) {
            return new FixedPointInt64(x._serializedValue + y._serializedValue);
        }

        /// <summary>
        /// Subtracts y from x. Performs saturating substraction, i.e. in case of overflow, 
        /// rounds to MinValue or MaxValue depending on sign of operands.
        /// </summary>
        public static FixedPointInt64 operator -(FixedPointInt64 x, FixedPointInt64 y) {
            return new FixedPointInt64(x._serializedValue - y._serializedValue);
        }

        /// <summary>
        /// Subtracts y from x witout performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static FixedPointInt64 OverflowSub(FixedPointInt64 x, FixedPointInt64 y) {
            var xl = x._serializedValue;
            var yl = y._serializedValue;
            var diff = xl - yl;
            // if signs of operands are different and signs of sum and x are different
            if ((((xl ^ yl) & (xl ^ diff)) & MIN_VALUE) != 0) {
                diff = xl < 0 ? MIN_VALUE : MAX_VALUE;
            }
            return new FixedPointInt64(diff);
        }

        /// <summary>
        /// Subtracts y from x witout performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static FixedPointInt64 FastSub(FixedPointInt64 x, FixedPointInt64 y) {
            return new FixedPointInt64(x._serializedValue - y._serializedValue);
        }

        static long AddOverflowHelper(long x, long y, ref bool overflow) {
            var sum = x + y;
            // x + y overflows if sign(x) ^ sign(y) != sign(sum)
            overflow |= ((x ^ y ^ sum) & MIN_VALUE) != 0;
            return sum;
        }

        public static FixedPointInt64 operator *(FixedPointInt64 x, FixedPointInt64 y) {
            var xl = x._serializedValue;
            var yl = y._serializedValue;

            var xlo = (ulong)(xl & 0x00000000FFFFFFFF);
            var xhi = xl >> FRACTIONAL_PLACES;
            var ylo = (ulong)(yl & 0x00000000FFFFFFFF);
            var yhi = yl >> FRACTIONAL_PLACES;

            var lolo = xlo * ylo;
            var lohi = (long)xlo * yhi;
            var hilo = xhi * (long)ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FRACTIONAL_PLACES;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FRACTIONAL_PLACES;

            var sum = (long)loResult + midResult1 + midResult2 + hiResult;
            FixedPointInt64 result;// = default(FP);
            result._serializedValue = sum;
            return result;
        }

        /// <summary>
        /// Performs multiplication without checking for overflow.
        /// Useful for performance-critical code where the values are guaranteed not to cause overflow
        /// </summary>
        public static FixedPointInt64 OverflowMul(FixedPointInt64 x, FixedPointInt64 y) {
            var xl = x._serializedValue;
            var yl = y._serializedValue;

            var xlo = (ulong)(xl & 0x00000000FFFFFFFF);
            var xhi = xl >> FRACTIONAL_PLACES;
            var ylo = (ulong)(yl & 0x00000000FFFFFFFF);
            var yhi = yl >> FRACTIONAL_PLACES;

            var lolo = xlo * ylo;
            var lohi = (long)xlo * yhi;
            var hilo = xhi * (long)ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FRACTIONAL_PLACES;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FRACTIONAL_PLACES;

            bool overflow = false;
            var sum = AddOverflowHelper((long)loResult, midResult1, ref overflow);
            sum = AddOverflowHelper(sum, midResult2, ref overflow);
            sum = AddOverflowHelper(sum, hiResult, ref overflow);

            bool opSignsEqual = ((xl ^ yl) & MIN_VALUE) == 0;

            // if signs of operands are equal and sign of result is negative,
            // then multiplication overflowed positively
            // the reverse is also true
            if (opSignsEqual) {
                if (sum < 0 || (overflow && xl > 0)) {
                    return MaxValue;
                }
            } else {
                if (sum > 0) {
                    return MinValue;
                }
            }

            // if the top 32 bits of hihi (unused in the result) are neither all 0s or 1s,
            // then this means the result overflowed.
            var topCarry = hihi >> FRACTIONAL_PLACES;
            if (topCarry != 0 && topCarry != -1 /*&& xl != -17 && yl != -17*/) {
                return opSignsEqual ? MaxValue : MinValue;
            }

            // If signs differ, both operands' magnitudes are greater than 1,
            // and the result is greater than the negative operand, then there was negative overflow.
            if (!opSignsEqual) {
                long posOp, negOp;
                if (xl > yl) {
                    posOp = xl;
                    negOp = yl;
                } else {
                    posOp = yl;
                    negOp = xl;
                }
                if (sum > negOp && negOp < -ONE && posOp > ONE) {
                    return MinValue;
                }
            }

            return new FixedPointInt64(sum);
        }

        /// <summary>
        /// Performs multiplication without checking for overflow.
        /// Useful for performance-critical code where the values are guaranteed not to cause overflow
        /// </summary>
        public static FixedPointInt64 FastMul(FixedPointInt64 x, FixedPointInt64 y) {
            var xl = x._serializedValue;
            var yl = y._serializedValue;

            var xlo = (ulong)(xl & 0x00000000FFFFFFFF);
            var xhi = xl >> FRACTIONAL_PLACES;
            var ylo = (ulong)(yl & 0x00000000FFFFFFFF);
            var yhi = yl >> FRACTIONAL_PLACES;

            var lolo = xlo * ylo;
            var lohi = (long)xlo * yhi;
            var hilo = xhi * (long)ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FRACTIONAL_PLACES;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FRACTIONAL_PLACES;

            var sum = (long)loResult + midResult1 + midResult2 + hiResult;
			FixedPointInt64 result;// = default(FP);
			result._serializedValue = sum;
			return result;
            //return new FP(sum);
        }

        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)] 
        public static int CountLeadingZeroes(ulong x) {
            int result = 0;
            while ((x & 0xF000000000000000) == 0) { result += 4; x <<= 4; }
            while ((x & 0x8000000000000000) == 0) { result += 1; x <<= 1; }
            return result;
        }

        public static FixedPointInt64 operator /(FixedPointInt64 x, FixedPointInt64 y) {
            var xl = x._serializedValue;
            var yl = y._serializedValue;

            if (yl == 0) {
                return MAX_VALUE;
                //throw new DivideByZeroException();
            }

            var remainder = (ulong)(xl >= 0 ? xl : -xl);
            var divider = (ulong)(yl >= 0 ? yl : -yl);
            var quotient = 0UL;
            var bitPos = NUM_BITS / 2 + 1;


            // If the divider is divisible by 2^n, take advantage of it.
            while ((divider & 0xF) == 0 && bitPos >= 4) {
                divider >>= 4;
                bitPos -= 4;
            }

            while (remainder != 0 && bitPos >= 0) {
                int shift = CountLeadingZeroes(remainder);
                if (shift > bitPos) {
                    shift = bitPos;
                }
                remainder <<= shift;
                bitPos -= shift;

                var div = remainder / divider;
                remainder = remainder % divider;
                quotient += div << bitPos;

                // Detect overflow
                if ((div & ~(0xFFFFFFFFFFFFFFFF >> bitPos)) != 0) {
                    return ((xl ^ yl) & MIN_VALUE) == 0 ? MaxValue : MinValue;
                }

                remainder <<= 1;
                --bitPos;
            }

            // rounding
            ++quotient;
            var result = (long)(quotient >> 1);
            if (((xl ^ yl) & MIN_VALUE) != 0) {
                result = -result;
            }

            return new FixedPointInt64(result);
        }

        public static FixedPointInt64 operator %(FixedPointInt64 x, FixedPointInt64 y) {
            return new FixedPointInt64(
                x._serializedValue == MIN_VALUE & y._serializedValue == -1 ?
                0 :
                x._serializedValue % y._serializedValue);
        }

        /// <summary>
        /// Performs modulo as fast as possible; throws if x == MinValue and y == -1.
        /// Use the operator (%) for a more reliable but slower modulo.
        /// </summary>
        public static FixedPointInt64 FastMod(FixedPointInt64 x, FixedPointInt64 y) {
            return new FixedPointInt64(x._serializedValue % y._serializedValue);
        }

        public static FixedPointInt64 operator -(FixedPointInt64 x) {
            return x._serializedValue == MIN_VALUE ? MaxValue : new FixedPointInt64(-x._serializedValue);
        }

        public static bool operator ==(FixedPointInt64 x, FixedPointInt64 y) {
            return x._serializedValue == y._serializedValue;
        }

        public static bool operator !=(FixedPointInt64 x, FixedPointInt64 y) {
            return x._serializedValue != y._serializedValue;
        }

        public static bool operator >(FixedPointInt64 x, FixedPointInt64 y) {
            return x._serializedValue > y._serializedValue;
        }

        public static bool operator <(FixedPointInt64 x, FixedPointInt64 y) {
            return x._serializedValue < y._serializedValue;
        }

        public static bool operator >=(FixedPointInt64 x, FixedPointInt64 y) {
            return x._serializedValue >= y._serializedValue;
        }

        public static bool operator <=(FixedPointInt64 x, FixedPointInt64 y) {
            return x._serializedValue <= y._serializedValue;
        }


        /// <summary>
        /// Returns the square root of a specified number.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The argument was negative.
        /// </exception>
        public static FixedPointInt64 Sqrt(FixedPointInt64 x) {
            var xl = x._serializedValue;
            if (xl < 0) {
                // We cannot represent infinities like Single and Double, and Sqrt is
                // mathematically undefined for x < 0. So we just throw an exception.
                throw new ArgumentOutOfRangeException("Negative value passed to Sqrt", "x");
            }

            var num = (ulong)xl;
            var result = 0UL;

            // second-to-top bit
            var bit = 1UL << (NUM_BITS - 2);

            while (bit > num) {
                bit >>= 2;
            }

            // The main part is executed twice, in order to avoid
            // using 128 bit values in computations.
            for (var i = 0; i < 2; ++i) {
                // First we get the top 48 bits of the answer.
                while (bit != 0) {
                    if (num >= result + bit) {
                        num -= result + bit;
                        result = (result >> 1) + bit;
                    } else {
                        result = result >> 1;
                    }
                    bit >>= 2;
                }

                if (i == 0) {
                    // Then process it again to get the lowest 16 bits.
                    if (num > (1UL << (NUM_BITS / 2)) - 1) {
                        // The remainder 'num' is too large to be shifted left
                        // by 32, so we have to add 1 to result manually and
                        // adjust 'num' accordingly.
                        // num = a - (result + 0.5)^2
                        //       = num + result^2 - (result + 0.5)^2
                        //       = num - result - 0.5
                        num -= result;
                        num = (num << (NUM_BITS / 2)) - 0x80000000UL;
                        result = (result << (NUM_BITS / 2)) + 0x80000000UL;
                    } else {
                        num <<= (NUM_BITS / 2);
                        result <<= (NUM_BITS / 2);
                    }

                    bit = 1UL << (NUM_BITS / 2 - 2);
                }
            }
            // Finally, if next bit would have been 1, round the result upwards.
            if (num > result) {
                ++result;
            }
            return new FixedPointInt64((long)result);
        }

        /// <summary>
        /// Returns the Sine of x.
        /// This function has about 9 decimals of accuracy for small values of x.
        /// It may lose accuracy as the value of x grows.
        /// Performance: about 25% slower than Math.Sin() in x64, and 200% slower in x86.
        /// </summary>
        public static FixedPointInt64 Sin(FixedPointInt64 x) {
            bool flipHorizontal, flipVertical;
            var clampedL = ClampSinValue(x._serializedValue, out flipHorizontal, out flipVertical);
            var clamped = new FixedPointInt64(clampedL);

            // Find the two closest values in the LUT and perform linear interpolation
            // This is what kills the performance of this function on x86 - x64 is fine though
            var rawIndex = FastMul(clamped, LutInterval);
            var roundedIndex = Round(rawIndex);
            var indexError = 0;//FastSub(rawIndex, roundedIndex);

            var nearestValue = new FixedPointInt64(SinLut[flipHorizontal ?
                SinLut.Length - 1 - (int)roundedIndex :
                (int)roundedIndex]);
            var secondNearestValue = new FixedPointInt64(SinLut[flipHorizontal ?
                SinLut.Length - 1 - (int)roundedIndex - Sign(indexError) :
                (int)roundedIndex + Sign(indexError)]);

            var delta = FastMul(indexError, FastAbs(FastSub(nearestValue, secondNearestValue)))._serializedValue;
            var interpolatedValue = nearestValue._serializedValue + (flipHorizontal ? -delta : delta);
            var finalValue = flipVertical ? -interpolatedValue : interpolatedValue;
            FixedPointInt64 a2 = new FixedPointInt64(finalValue);
            return a2;
        }

        /// <summary>
        /// Returns a rough approximation of the Sine of x.
        /// This is at least 3 times faster than Sin() on x86 and slightly faster than Math.Sin(),
        /// however its accuracy is limited to 4-5 decimals, for small enough values of x.
        /// </summary>
        public static FixedPointInt64 FastSin(FixedPointInt64 x) {
            bool flipHorizontal, flipVertical;
            var clampedL = ClampSinValue(x._serializedValue, out flipHorizontal, out flipVertical);

            // Here we use the fact that the SinLut table has a number of entries
            // equal to (PI_OVER_2 >> 15) to use the angle to index directly into it
            var rawIndex = (uint)(clampedL >> 15);
            if (rawIndex >= LUT_SIZE) {
                rawIndex = LUT_SIZE - 1;
            }
            var nearestValue = SinLut[flipHorizontal ?
                SinLut.Length - 1 - (int)rawIndex :
                (int)rawIndex];
            return new FixedPointInt64(flipVertical ? -nearestValue : nearestValue);
        }



        //[MethodImplAttribute(MethodImplOptions.AggressiveInlining)] 
        public static long ClampSinValue(long angle, out bool flipHorizontal, out bool flipVertical) {
            // Clamp value to 0 - 2*PI using modulo; this is very slow but there's no better way AFAIK
            var clamped2Pi = angle % PI_TIMES_2;
            if (angle < 0) {
                clamped2Pi += PI_TIMES_2;
            }

            // The LUT contains values for 0 - PiOver2; every other value must be obtained by
            // vertical or horizontal mirroring
            flipVertical = clamped2Pi >= PI;
            // obtain (angle % PI) from (angle % 2PI) - much faster than doing another modulo
            var clampedPi = clamped2Pi;
            while (clampedPi >= PI) {
                clampedPi -= PI;
            }
            flipHorizontal = clampedPi >= PI_OVER_2;
            // obtain (angle % PI_OVER_2) from (angle % PI) - much faster than doing another modulo
            var clampedPiOver2 = clampedPi;
            if (clampedPiOver2 >= PI_OVER_2) {
                clampedPiOver2 -= PI_OVER_2;
            }
            return clampedPiOver2;
        }

        /// <summary>
        /// Returns the cosine of x.
        /// See Sin() for more details.
        /// </summary>
        public static FixedPointInt64 Cos(FixedPointInt64 x) {
            var xl = x._serializedValue;
            var rawAngle = xl + (xl > 0 ? -PI - PI_OVER_2 : PI_OVER_2);
            FixedPointInt64 a2 = Sin(new FixedPointInt64(rawAngle));
            return a2;
        }

        /// <summary>
        /// Returns a rough approximation of the cosine of x.
        /// See FastSin for more details.
        /// </summary>
        public static FixedPointInt64 FastCos(FixedPointInt64 x) {
            var xl = x._serializedValue;
            var rawAngle = xl + (xl > 0 ? -PI - PI_OVER_2 : PI_OVER_2);
            return FastSin(new FixedPointInt64(rawAngle));
        }

        /// <summary>
        /// Returns the tangent of x.
        /// </summary>
        /// <remarks>
        /// This function is not well-tested. It may be wildly inaccurate.
        /// </remarks>
        public static FixedPointInt64 Tan(FixedPointInt64 x) {
            var clampedPi = x._serializedValue % PI;
            var flip = false;
            if (clampedPi < 0) {
                clampedPi = -clampedPi;
                flip = true;
            }
            if (clampedPi > PI_OVER_2) {
                flip = !flip;
                clampedPi = PI_OVER_2 - (clampedPi - PI_OVER_2);
            }

            var clamped = new FixedPointInt64(clampedPi);

            // Find the two closest values in the LUT and perform linear interpolation
            var rawIndex = FastMul(clamped, LutInterval);
            var roundedIndex = Round(rawIndex);
            var indexError = FastSub(rawIndex, roundedIndex);

            var nearestValue = new FixedPointInt64(TanLut[(int)roundedIndex]);
            var secondNearestValue = new FixedPointInt64(TanLut[(int)roundedIndex + Sign(indexError)]);

            var delta = FastMul(indexError, FastAbs(FastSub(nearestValue, secondNearestValue)))._serializedValue;
            var interpolatedValue = nearestValue._serializedValue + delta;
            var finalValue = flip ? -interpolatedValue : interpolatedValue;
            FixedPointInt64 a2 = new FixedPointInt64(finalValue);
            return a2;
        }

        public static FixedPointInt64 Atan(FixedPointInt64 y) {
            return Atan2(y, 1);
        }

        public static FixedPointInt64 Atan2(FixedPointInt64 y, FixedPointInt64 x) {
            var yl = y._serializedValue;
            var xl = x._serializedValue;
            if (xl == 0) {
                if (yl > 0) {
                    return PiOver2;
                }
                if (yl == 0) {
                    return Zero;
                }
                return -PiOver2;
            }
            FixedPointInt64 atan;
            var z = y / x;

            FixedPointInt64 sm = FixedPointInt64.EN2 * 28;
            // Deal with overflow
            if (One + sm * z * z == MaxValue) {
                return y < Zero ? -PiOver2 : PiOver2;
            }

            if (Abs(z) < One) {
                atan = z / (One + sm * z * z);
                if (xl < 0) {
                    if (yl < 0) {
                        return atan - Pi;
                    }
                    return atan + Pi;
                }
            } else {
                atan = PiOver2 - z / (z * z + sm);
                if (yl < 0) {
                    return atan - Pi;
                }
            }
            return atan;
        }

        public static FixedPointInt64 Asin(FixedPointInt64 value) {
            return FastSub(PiOver2, Acos(value));
        }

        public static FixedPointInt64 Acos(FixedPointInt64 value) {
            if (value == 0) {
                return FixedPointInt64.PiOver2;
            }

            bool flip = false;
            if (value < 0) {
                value = -value;
                flip = true;
            }

            // Find the two closest values in the LUT and perform linear interpolation
            var rawIndex = FastMul(value, LUT_SIZE);
            var roundedIndex = Round(rawIndex);
            if (roundedIndex >= LUT_SIZE) {
                roundedIndex = LUT_SIZE - 1;
            }

            var indexError = FastSub(rawIndex, roundedIndex);
            var nearestValue = new FixedPointInt64(AcosLut[(int)roundedIndex]);

            var nextIndex = (int)roundedIndex + Sign(indexError);
            if (nextIndex >= LUT_SIZE) {
                nextIndex = LUT_SIZE - 1;
            }

            var secondNearestValue = new FixedPointInt64(AcosLut[nextIndex]);

            var delta = FastMul(indexError, FastAbs(FastSub(nearestValue, secondNearestValue)))._serializedValue;
            FixedPointInt64 interpolatedValue = new FixedPointInt64(nearestValue._serializedValue + delta);
            FixedPointInt64 finalValue = flip ? (FixedPointInt64.Pi - interpolatedValue) : interpolatedValue;

            return finalValue;
        }

        public static implicit operator FixedPointInt64(long value) {
            return new FixedPointInt64(value * ONE);
        }

        public static explicit operator long(FixedPointInt64 value) {
            return value._serializedValue >> FRACTIONAL_PLACES;
        }

        public static implicit operator FixedPointInt64(float value) {
            return new FixedPointInt64((long)(value * ONE));
        }

        public static explicit operator float(FixedPointInt64 value) {
            return (float)value._serializedValue / ONE;
        }

        public static implicit operator FixedPointInt64(double value) {
            return new FixedPointInt64((long)(value * ONE));
        }

        public static explicit operator double(FixedPointInt64 value) {
            return (double)value._serializedValue / ONE;
        }

        public static explicit operator FixedPointInt64(decimal value) {
            return new FixedPointInt64((long)(value * ONE));
        }

        public static implicit operator FixedPointInt64(int value) {
            return new FixedPointInt64(value * ONE);
        }

        public static explicit operator decimal(FixedPointInt64 value) {
            return (decimal)value._serializedValue / ONE;
        }

        public float AsFloat() {
            return (float) this;
        }

        public int AsInt() {
            return (int) this;
        }

        public long AsLong() {
            return (long)this;
        }

        public double AsDouble() {
            return (double)this;
        }

        public decimal AsDecimal() {
            return (decimal)this;
        }

        public static float ToFloat(FixedPointInt64 value) {
            return (float)value;
        }

        public static int ToInt(FixedPointInt64 value) {
            return (int)value;
        }

        public static FixedPointInt64 FromFloat(float value) {
            return (FixedPointInt64)value;
        }

        public static bool IsInfinity(FixedPointInt64 value) {
            return value == NegativeInfinity || value == PositiveInfinity;
        }

        public static bool IsNaN(FixedPointInt64 value) {
            return value == NaN;
        }

        public override bool Equals(object obj) {
            return obj is FixedPointInt64 && ((FixedPointInt64)obj)._serializedValue == _serializedValue;
        }

        public override int GetHashCode() {
            return _serializedValue.GetHashCode();
        }

        public bool Equals(FixedPointInt64 other) {
            return _serializedValue == other._serializedValue;
        }

        public int CompareTo(FixedPointInt64 other) {
            return _serializedValue.CompareTo(other._serializedValue);
        }

        public override string ToString() {
            return ((float)this).ToString();
        }

        public static FixedPointInt64 FromRaw(long rawValue) {
            return new FixedPointInt64(rawValue);
        }

        internal static void GenerateAcosLut() {
            using (var writer = new StreamWriter("Fix64AcosLut.cs")) {
                writer.Write(
@"namespace TrueSync {
    partial struct FP {
        public static readonly long[] AcosLut = new[] {");
                int lineCounter = 0;
                for (int i = 0; i < LUT_SIZE; ++i) {
                    var angle = i / ((float)(LUT_SIZE - 1));
                    if (lineCounter++ % 8 == 0) {
                        writer.WriteLine();
                        writer.Write("            ");
                    }
                    var acos = BlueNoah.Math.FixedPoint.FixedPointMath.Acos(angle);
                    var rawValue = ((FixedPointInt64)acos)._serializedValue;
                    writer.Write(string.Format("0x{0:X}L, ", rawValue));
                }
                writer.Write(
@"
        };
    }
}");
            }
        }

        internal static void GenerateSinLut() {
            using (var writer = new StreamWriter("Fix64SinLut.cs")) {
                writer.Write(
@"namespace FixMath.NET {
    partial struct Fix64 {
        public static readonly long[] SinLut = new[] {");
                int lineCounter = 0;
                for (int i = 0; i < LUT_SIZE; ++i) {
                    var angle = i * BlueNoah.Math.FixedPoint.FixedPointInt64.PI * 0.5 / (LUT_SIZE - 1);
                    if (lineCounter++ % 8 == 0) {
                        writer.WriteLine();
                        writer.Write("            ");
                    }
                    var sin = BlueNoah.Math.FixedPoint.FixedPointMath.Sin(angle);
                    var rawValue = ((FixedPointInt64)sin)._serializedValue;
                    writer.Write(string.Format("0x{0:X}L, ", rawValue));
                }
                writer.Write(
@"
        };
    }
}");
            }
        }

        internal static void GenerateTanLut() {
            using (var writer = new StreamWriter("Fix64TanLut.cs")) {
                writer.Write(
@"namespace FixMath.NET {
    partial struct Fix64 {
        public static readonly long[] TanLut = new[] {");
                int lineCounter = 0;
                for (int i = 0; i < LUT_SIZE; ++i) {
                    var angle = i * BlueNoah.Math.FixedPoint.FixedPointInt64.PI * 0.5 / (LUT_SIZE - 1);
                    if (lineCounter++ % 8 == 0) {
                        writer.WriteLine();
                        writer.Write("            ");
                    }
                    var tan = BlueNoah.Math.FixedPoint.FixedPointMath.Tan(angle);
                    if (tan > (double)MaxValue || tan < 0.0) {
                        tan = (double)MaxValue;
                    }
                    var rawValue = (((decimal)tan > (decimal)MaxValue || tan < 0.0) ? MaxValue : (FixedPointInt64)tan)._serializedValue;
                    writer.Write(string.Format("0x{0:X}L, ", rawValue));
                }
                writer.Write(
@"
        };
    }
}");
            }
        }

        /// <summary>
        /// The underlying integer representation
        /// </summary>
        public long RawValue { get { return _serializedValue; } }

        /// <summary>
        /// This is the constructor from raw value; it can only be used interally.
        /// </summary>
        /// <param name="rawValue"></param>
        FixedPointInt64(long rawValue) {
            _serializedValue = rawValue;
        }

        public FixedPointInt64(int value) {
            _serializedValue = value * ONE;
        }
    }
}