/*
 * Copyright 2010 The Closure Compiler Authors.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// import com.google.common.collect.Lists;
// import java.util.List;

namespace ClosureSourceMaps
{
    /// <summary>
    /// Class for parsing the line maps in SourceMap v2.
    /// 
    /// @author johnlenz@google.com (John Lenz)
    /// @author jschorr@google.com (Joseph Schorr)
    /// </summary>
    class SourceMapLineDecoder
    {
        /// <summary>
        /// Decodes a line in a character map into a list of mapping IDs.
        /// </summary>
        /// <param name="lineSource"></param>
        /// <returns></returns>
        private static List<Int32> decodeLine(string lineSource) 
        {
            return decodeLine(new StringParser(lineSource));
        }

        private SourceMapLineDecoder() {}

        private static LineEntry decodeLineEntry(string input, int lastId) 
        {
            return decodeLineEntry(new StringParser(input), lastId);
        }

        private static LineEntry decodeLineEntry(StringParser reader, int lastId) 
        {
            int repDigits = 0;
            
            // Determine the number of digits used for the repetition count.
            // Each "!" indicates another base64 digit.
            for (char peek = reader.Peek(); peek == '!'; peek = reader.Peek()) 
            {
                ++repDigits;
                reader.Next(); // consume the "!"
            }

            int idDigits = 0;
            int reps = 0;
            if (repDigits == 0) 
            {
                // No repetition digit escapes, so the next character represents the
                // number of digits in the id (bottom 2 bits) and the number of
                // repetitions (top 4 digits).
                char digit = reader.Next();
                int value = addBase64Digit(digit, 0);
                reps = (value >> 2);
                idDigits = (value & 3);
            } 
            else 
            {
                char digit = reader.Next();
                idDigits = addBase64Digit(digit, 0);

                int value = 0;
                for (int i = 0; i < repDigits; ++i) 
                {
                    digit = reader.Next();
                    value = addBase64Digit(digit, value);
                }
                reps = value;
            }

            // Adjust for 1 offset encoding.
            reps += 1;
            idDigits += 1;

            // Decode the id token.
            int val = 0;
            for (int i = 0; i < idDigits; ++i)
            {
                char digit = reader.Next();
                val = addBase64Digit(digit, val);
            }
            int mappingId = getIdFromRelativeId(val, idDigits, lastId);
            return new LineEntry(mappingId, reps);
        }

        private static List<Int32> decodeLine(StringParser reader) 
        {
            List<Int32> result = new List<Int32>(512);
            int lastId = 0;
            while (reader.HasNext()) 
            {
                LineEntry entry = decodeLineEntry(reader, lastId);
                lastId = entry.id;

                for (int i=0; i < entry.reps; ++i) 
                {
                    result.Add(entry.id);
                } 
            }

            return result;
        }

        /// <summary>
        /// Build base64 number a digit at a time, most significant digit first.
        /// </summary>
        /// <param name="digit"></param>
        /// <param name="previousValue"></param>
        /// <returns></returns>
        private static int addBase64Digit(char digit, int previousValue)
        {
            return (previousValue * 64) + Base64.FromBase64(digit);
        }

        /// <summary></summary>
        /// <param name="rawId"></param>
        /// <param name="digits"></param>
        /// <param name="lastId"></param>
        /// <returns>The id from the relative id.</returns>
        private static int getIdFromRelativeId(int rawId, int digits, int lastId)
        {
            // The value range depends on the number of digits
            int basis = 1 << (digits * 6);
            return ((rawId >= basis/2) ? rawId - basis : rawId) + lastId;
        }

        /// <summary>
        /// Simple class for tracking a single entry in a line map.
        /// </summary>
        class LineEntry 
        {
            public readonly int id;
            public readonly int reps;
            public LineEntry(int id, int reps)
            {
                this.id = id;
                this.reps = reps;
            }
        }

        /// <summary>
        /// A simple class for maintaining the current location in the input.
        /// </summary>
        class StringParser 
        {
            readonly string content;
            private int current = 0;

            public StringParser(string content)
            {
                this.content = content;
            }

            public char Next() 
            {
                return content[current++];
            }

            public char Peek()
            {
                return content[current];
            }

            public bool HasNext()
            {
                return current < content.Length -1;
            }
        }
    }
}
