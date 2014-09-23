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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.IO;

/**
 * @author johnlenz@google.com (John Lenz)
 */

namespace ClosureSourceMaps
{
    class Util
    {
        private static readonly char[] HexChars = { '0', '1', '2', '3', '4', '5', '6', '7',
                                                 '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        /// <summary>
        /// Escapes the given string to a double quoted (") JavaScript/JSON string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string escapeString(string s) 
        {
            return escapeString(s, '"',  "\\\"", "\'", "\\\\", null);
        }

        /// <summary>
        /// Helper to escape JavaScript string as well as regular expression.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="quote"></param>
        /// <param name="doublequoteEscape"></param>
        /// <param name="singlequoteEscape"></param>
        /// <param name="backslashEscape"></param>
        /// <param name="outputCharsetEncoder"></param>
        /// <returns></returns>
        internal static string escapeString(string s, char quote, string doublequoteEscape,
                                   string singlequoteEscape, string backslashEscape,
                                   Encoding outputCharsetEncoder)
        {
            #warning Check out whether outputCharsetEncoder is ASCII
            StringBuilder sb = new StringBuilder(s.Length + 2);
            sb.Append(quote);
            for (int i = 0; i < s.Length; i++) 
            {
                char c = s[i];
                switch (c) 
                {
                    case '\n': 
                        sb.Append("\\n"); 
                        break;
                    case '\r': 
                        sb.Append("\\r"); 
                        break;
                    case '\t': 
                        sb.Append("\\t"); 
                        break;
                    case '\\': 
                        sb.Append(backslashEscape); 
                        break;
                    case '\"': 
                        sb.Append(doublequoteEscape); 
                        break;
                    case '\'': 
                        sb.Append(singlequoteEscape); 
                        break;
                    case '>':                       // Break --> into --\> or ]]> into ]]\>
                        if (i >= 2 && ((s[i - 1] == '-' && s[i - 2] == '-') ||
                                       (s[i - 1] == ']' && s[i - 2] == ']'))) 
                        {
                            sb.Append("\\>");
                        } 
                        else 
                        {
                            sb.Append(c);
                        }
                        break;
                    case '<':                       
                        // Break </script into <\/script
                        const string EndScript = "/script";
                        // Break <!-- into <\!--
                        const string StartComment = "!--";

                        if ((s.Substring(i + 1)).Equals(EndScript, StringComparison.OrdinalIgnoreCase)) 
                        {
                            sb.Append("<\\");
                        }
                        else if ((s.Substring(i + 1)).Equals(StartComment)) 
                        {
                            sb.Append("<\\");
                        } 
                        else 
                        {
                            sb.Append(c);
                        }
                        break;
                    default:
                        // If we're given an outputCharsetEncoder, then check if the
                        //  character can be represented in this character set.
                        if (outputCharsetEncoder != null) 
                        {
                            if (outputCharsetEncoder.canEncode(c)) 
                            {
                                sb.Append(c);
                            } 
                            else 
                            {
                                // Unicode-escape the character.
                                appendCharAsHex(sb, c);
                            }
                        } 
                        else 
                        {
                            // No charsetEncoder provided - pass straight Latin characters
                            // through, and escape the rest.  Doing the explicit character
                            // check is measurably faster than using the CharsetEncoder.
                            if (c > 0x1f && c <= 0x7f) 
                            {
                                sb.Append(c);
                            } 
                            else 
                            {
                                // Other characters can be misinterpreted by some JS parsers,
                                // or perhaps mangled by proxies along the way,
                                // so we play it safe and Unicode escape them.
                                appendCharAsHex(sb, c);
                            }
                        }
                }
            }
            sb.Append(quote);
            return sb.ToString();
        }

        /// <summary>
        /// <see cref="appendHexJavaScriptRepresentation(StringBuilder, int)"/>
        /// </summary>
        /// <param name="?"></param>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private static void appendCharAsHex(StringBuilder sb, char c) 
        {
            try 
            {
                appendHexJavaScriptRepresentation(sb, (int)c);
            }
            catch (InvalidOperationException ex) 
            {
                // StringBuilder does not throw IOException.
                throw new Exception("Runtime exception", ex);
            }
        }

        /// <summary>
        /// Returns a JavaScript representation of the character in a hex escaped format.
        /// </summary>
        /// <param name="output">The buffer to which the hex representation should be appended.</param>
        /// <param name="codePoint">The code point to append.</param>
        private static void appendHexJavaScriptRepresentation(StringBuilder output, int codePoint)
        {
            if (codePoint >= 0x10000 && codePoint <= 0x10FFFF) 
            {
                // Handle supplementary Unicode values which are not representable in
                // JavaScript.  We deal with these by escaping them as two 4B sequences
                // so that they will round-trip properly when sent from Java to JavaScript
                // and back.
                string surrogates = Char.ConvertFromUtf32(codePoint);
                appendHexJavaScriptRepresentation(output, surrogates[0]);
                appendHexJavaScriptRepresentation(output, surrogates[1]);
                return;
            }
            output.Append("\\u")
                    .Append(HexChars[(int)((uint)codePoint >> 12) & 0xf])
                    .Append(HexChars[(int)((uint)codePoint >> 8) & 0xf])
                    .Append(HexChars[(int)((uint)codePoint >> 4) & 0xf])
                    .Append(HexChars[codePoint & 0xf]);
        }
    }
}
