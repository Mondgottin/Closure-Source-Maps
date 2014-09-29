/*
 * Copyright 2011 The Closure Compiler Authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace ClosureSourceMaps
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// We encode our variable length numbers as base64 encoded strings with
    /// the least significant digit coming first.  Each base64 digit encodes
    /// a 5-bit value (0-31) and a continuation bit.  Signed values can be
    /// represented by using the least significant bit of the value as the
    /// sign bit.
    ///
    /// @author johnlenz@google.com (John Lenz)
    /// </summary>
    public static class Base64Vlq
    {
        // A Base64 VLQ digit can represent 5 bits, so it is base-32.
        private const int vlqBaseShift = 5;
        
        private const int vlqBase = 1 << vlqBaseShift;

        // A mask of bits for a VLQ digit (11111), 31 decimal.
        private  const int vlqBaseMask = vlqBase - 1;

        // The continuation bit is the 6th bit.
        private const int vlqContinuationBit = vlqBase;

        /// <summary>
        /// Converts from a two-complement value to a value where the sign bit 
        /// is placed in the least significant bit.  For example, as decimals:
        /// 1 becomes 2 (10 binary), -1 becomes 3 (11 binary)
        /// 2 becomes 4 (100 binary), -2 becomes 5 (101 binary)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int toVlqSigned(int value) 
        {
            if (value < 0) 
                return ((-value) << 1) + 1;
            return (value << 1) + 0;    
        }

        /// <summary>
        /// Converts to a two-complement value from a value where the sign bit
        /// is placed in the least significant bit.  For example, as decimals:
        /// 2 (10 binary) becomes 1, 3 (11 binary) becomes -1
        /// 4 (100 binary) becomes 2, 5 (101 binary) becomes -2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int fromVlqSigned(int value) 
        {
            bool negate = (value & 1) == 1;
            value = value >> 1;
            return negate ? -value : value;
        }

        /// <summary>
        /// Writes a VLQ encoded value to the provide appendable.
        /// @throws IOException
        /// </summary>
        /// <param name="?"></param>
        public static void Encode(StringBuilder output, int value)
        {
            value = toVlqSigned(value);
            do 
            {
                int digit = value & vlqBaseMask;
                value =  value >> vlqBaseShift;
                if (value > 0) 
                {
                    digit |= vlqContinuationBit;
                }
                output.Append(Base64.ToBase64(digit));
            } while (value > 0);
        }

        /// <summary>
        /// Decodes the next VLQValue from the provided ICharIterator  
        /// </summary>
        public static int Decode(IEnumerable<char> chars) 
        {
            int result = 0;
            int shift = 0;
            foreach (var c in chars) {
                int digit = Base64.FromBase64(c);
                bool continuation = (digit & vlqContinuationBit) != 0;
                digit &= vlqBaseMask;
                result = result + (digit << shift);
                shift = shift + vlqBaseShift;
                if (!continuation)
                    break;
            }
            
            return fromVlqSigned(result);
        }
    }
}