using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BigNum
{
    private List<uint> digits = new List<uint>();
    private static string hexDigits = "0123456789ABCDEF";

    public BigNum(string number, int baseNum = 10)
    {
        number = number.Trim();

        for (int i = number.Length - 1; i >= 0; i--)
        {
            char currentChar = Char.ToUpper(number[i]);
            int value = hexDigits.IndexOf(currentChar);
            if (value < 0 || value >= baseNum)
            {
                Console.WriteLine($"Invalid character found: {currentChar}");
                throw new ArgumentException("Invalid character in number");
            }
            digits.Add((uint)value);
        }
    }

    public static BigNum Add(BigNum num1, BigNum num2)
    {
        BigNum result = new BigNum("");
        (BigNum larger, BigNum smaller) = num1.digits.Count > num2.digits.Count ? (num1, num2) : (num2, num1);

        uint carry = 0;
        for (int i = 0; i < larger.digits.Count; i++)
        {
            uint sum = larger.digits[i] + (i < smaller.digits.Count ? smaller.digits[i] : 0) + carry;
            carry = sum >> 4;
            result.digits.Add(sum & 0xF);
        }

        if (carry > 0)
        {
            result.digits.Add(carry);
        }

        return result;
    }

    public static BigNum Sub(BigNum num1, BigNum num2)
    {
        BigNum result = new BigNum("");
        uint borrow = 0;

        for (int i = 0; i < num1.digits.Count; i++)
        {
            uint diff = num1.digits[i] - (i < num2.digits.Count ? num2.digits[i] : 0) - borrow;
            if (diff > num1.digits[i])
            {
                borrow = 1;
                diff += 16;
            }
            else
            {
                borrow = 0;
            }
            result.digits.Add(diff & 0xF);
        }

        while (result.digits.Count > 1 && result.digits.Last() == 0)
        {
            result.digits.RemoveAt(result.digits.Count - 1);
        }

        return result;
    }

    public static int BitLength(BigNum num)
    {
        int bitLength = 0;
        if (num.digits.Count == 0)
        {
            return 0;
        }
        uint topDigit = num.digits.Last();
        while (topDigit != 0)
        {
            bitLength++;
            topDigit >>= 1;
        }
        bitLength += (num.digits.Count - 1) * 4;
        return bitLength;
    }

    public static BigNum LongShiftBitsToHigh(BigNum num, int shiftBits)
    {
        int shiftDigits = shiftBits / 4;
        int shiftRemainder = shiftBits % 4;

        List<uint> shiftedDigits = new List<uint>(Enumerable.Repeat((uint)0, shiftDigits + num.digits.Count));

        for (int i = 0; i < num.digits.Count; i++)
        {
            shiftedDigits[i + shiftDigits] = num.digits[i];
        }

        if (shiftRemainder > 0)
        {
            uint carry = 0;
            for (int i = 0; i < shiftedDigits.Count; i++)
            {
                uint newCarry = shiftedDigits[i] >> (4 - shiftRemainder);
                shiftedDigits[i] = ((shiftedDigits[i] << shiftRemainder) | carry) & 0xF;
                carry = newCarry;
            }

            if (carry > 0)
            {
                shiftedDigits.Add(carry);
            }
        }

        num.digits = shiftedDigits;
        return num;
    }

    public static (BigNum, BigNum) LongDivMod(BigNum A, BigNum B)
    {
        if (LongCmp(B, new BigNum("0")) == 0)
        {
            throw new DivideByZeroException();
        }

        int k = BitLength(B);
        BigNum R = new BigNum(A.ToString(16), 16);  
        BigNum Q = new BigNum("");

        while (LongCmp(R, B) >= 0)
        {
            int t = BitLength(R);
            BigNum C = LongShiftBitsToHigh(new BigNum(B.ToString(16), 16), t - k); 
            if (LongCmp(R, C) < 0)
            {
                t -= 1;
                C = LongShiftBitsToHigh(new BigNum(B.ToString(16), 16), t - k); 
            }
            Q = Add(Q, LongShiftBitsToHigh(new BigNum("1"), t - k));
            R = Sub(R, C);
        }
        return (Q, R);
    }

    public static int LongCmp(BigNum A, BigNum B)
    {
        if (A.digits.Count != B.digits.Count)
        {
            return A.digits.Count.CompareTo(B.digits.Count);
        }
        for (int i = A.digits.Count - 1; i >= 0; i--)
        {
            if (A.digits[i] != B.digits[i])
            {
                return A.digits[i].CompareTo(B.digits[i]);
            }
        }
        return 0;
    }

    public string ToString(int baseNum = 10)
    {
        var sb = new StringBuilder();
        bool nonZeroFound = false;
        for (int i = digits.Count - 1; i >= 0; i--)
        {
            if (digits[i] != 0)
            {
                nonZeroFound = true;
            }
            if (nonZeroFound)
            {
                sb.Append(hexDigits[(int)digits[i]]);
            }
        }
        return sb.Length > 0 ? sb.ToString() : "0";
    }

    public static BigNum Mul(BigNum num1, BigNum num2)
    {
        BigNum result = new BigNum("");
        result.digits = new List<uint>(new uint[num1.digits.Count + num2.digits.Count]);

        for (int i = 0; i < num1.digits.Count; i++)
        {
            if (num1.digits[i] == 0) continue;

            uint carry = 0;
            for (int j = 0; j < num2.digits.Count; j++)
            {
                uint product = num1.digits[i] * num2.digits[j] + result.digits[i + j] + carry;
                result.digits[i + j] = product & 0xF;
                carry = product >> 4;
            }

            if (carry > 0)
            {
                result.digits[i + num2.digits.Count] = carry;
            }
        }

        while (result.digits.Count > 1 && result.digits.Last() == 0)
        {
            result.digits.RemoveAt(result.digits.Count - 1);
        }

        return result;
    }
    public static BigNum GCD(BigNum a, BigNum b)
    {
        if (LongCmp(b, new BigNum("0")) == 0)
        {
            return a;
        }
        return GCD(b, LongDivMod(a, b).Item2);
    }
    public static BigNum LCM(BigNum a, BigNum b)
    {
        BigNum gcd = GCD(a, b);
        BigNum temp = Mul(a, b);
        BigNum lcm = BigNum.LongDivMod(temp, gcd).Item1;
        return lcm;
    }
    public static BigNum AddMod(BigNum num1, BigNum num2, BigNum mod)
    {
        BigNum result = Add(num1, num2);

        return LongDivMod(result, mod).Item2;

    }

    public static BigNum SubMod(BigNum num1, BigNum num2, BigNum mod)
    {
        BigNum temp = new BigNum("0");
        if (LongCmp(num1, num2) <= 0)
        {
            temp = Add(num1, mod);
            while (LongCmp(temp, num2) < 0)
            {
                temp = Add(temp, mod);
            }
        }
        else
        {
            temp = num1;
        }

        BigNum result = Sub(temp, num2);
        return LongDivMod(result, mod).Item2;
    }

    public static BigNum MulMod(BigNum num1, BigNum num2, BigNum mod)
    {
        BigNum result = Mul(num1, num2);
        return LongDivMod(result, mod).Item2;
    }
    
    public static BigNum SquareMod(BigNum num, BigNum mod)
    {
        BigNum result = Mul(num, num);
        return LongDivMod(result, mod).Item2;
    }
    
    public static BigNum PowMod(BigNum baseNum, BigNum exponent, BigNum mod)
    {
        if (LongCmp(exponent, new BigNum("0")) == 0)
        {
            return new BigNum("1");
        }
        BigNum result = new BigNum("1");
        while (LongCmp(exponent, new BigNum("0")) > 0)
        {
            if ((LongDivMod(exponent, new BigNum("2")).Item2).ToString(16) == "1")
            {
                result = LongDivMod(Mul(result, baseNum), mod).Item2;
            }
            exponent = LongDivMod(exponent, new BigNum("2")).Item1;
            baseNum = LongDivMod(Mul(baseNum, baseNum), mod).Item2;
        }
        return result;
    }
}

namespace lab12
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BigNum bi1 = new BigNum("658ec5e5d8f126d692936c196bc1be68fdbc7483d7f1035c8e636c36ed5d4d0f0f69a0fa8158f4d0967e3ebd23aa4081f14540fb4f4724140e38ab2a40d33e807028b2f40b1e5362c392348d7cd81dde9ec6a79e1f4abb8b706ea0d63d30f0663a2ca1ff673d8889bc315853fca663a2c05f46c87657f0fea4ec2aafe8bbf3ac", 16);
            BigNum bi2 = new BigNum("f5aa1bd307eedf96db133a218aa605a0edb6de4fa9d43d100cd75990b3c9b72347ff83796c2f886b4a58f312b8bdbdbcaf500790eee9add7165837032d2bd268e9a73cb50dca7a065abb515a18f8783c9f1bdaa5400a2b2f06cc42a2b38f41599d5bab219aaff407f6c283d479de944f8d039233709e21e30aa380b98026ac33", 16);
            BigNum bi3 = new BigNum("5bffd2ad88c901a224caba35ba09c26ae6be9c06dae976a0fc91b7e6f5076559813cb94d2b33e86c9bf1afabc4598e769fd3e7c27bc45bbeab5e78ce48fabe89ef514ff65c64da6e6858a91600de5b4091b219b88ff502fd166ea1ef805e37bc0695d17348823898c0a7bfe217d7980ca93b724b2aecec6c4f0165e9b30002e2", 16);
            BigNum bi4 = new BigNum("69fd3e7c27bc45bbeab5e78ce48fabe89ef514ff65c64da6e6858a91600de5b4091b219b88ff502fd166ea1ef805e37bc0695d17348823898c0a7bfe217d7980ca93b724b2aecec6c4f0165e9b30002e2", 16);
            
            BigNum sum = BigNum.Add(bi1, bi2);
            Console.WriteLine("Add result: " + sum.ToString(16));
            
            BigNum difference = BigNum.Sub(bi2, bi1);
            Console.WriteLine("Sub result: " + difference.ToString(16));
            
           BigNum product = BigNum.Mul(bi1, bi2);
           Console.WriteLine("Mul result: " + product.ToString(16));
            
            Console.WriteLine("Division:\n");
            var result = BigNum.LongDivMod(bi1, bi4);
            BigNum Q = result.Item1;
            BigNum R = result.Item2;

            Console.WriteLine("Q: " + Q.ToString(16));
            Console.WriteLine("R: " + R.ToString(16));
            
            BigNum gcd = BigNum.GCD(bi1, bi2);
            Console.WriteLine("gcd: " + gcd.ToString(16));
            
            BigNum lcm = BigNum.LCM(bi1, bi2);
            Console.WriteLine("Lcm: " + lcm.ToString(16)); 
            
            BigNum resultAddMod = BigNum.AddMod(bi1, bi2, bi3);
            Console.WriteLine($"AddMod: {resultAddMod.ToString(16)}");

            BigNum resultSubMod = BigNum.SubMod(bi1, bi2, bi3);
            Console.WriteLine($"SubMod: {resultSubMod.ToString(16)}");

            BigNum resultMulMod = BigNum.MulMod(bi1, bi2, bi3);
            Console.WriteLine($"MulMod: {resultMulMod.ToString(16)}");

             BigNum resultPowMod = BigNum.PowMod( bi1, bi2, bi3);
            Console.WriteLine($"PowMod: {resultPowMod.ToString(16)}");
            
        }
    }
}
