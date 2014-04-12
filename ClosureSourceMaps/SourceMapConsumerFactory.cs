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

// import org.json.JSONException;
// import org.json.JSONObject;

namespace ClosureSourceMaps
{
    /// <summary>
    /// Detect and parse the provided source map
    /// 
    /// @author johnlenz@google.com (John Lenz)
    /// </summary>
    public class SourceMapConsumerFactory
    {
        /** not constructible */
        private SourceMapConsumerFactory() { }

        /// <summary></summary>
        /// <param name="contents">The string representing the source map file contents.</param>
        /// <returns>The parsed source map.</returns>
        public static SourceMapping Parse(string contents)
        {
            return Parse(contents, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="contents">The string representing the source map file contents.</param>
        /// <param name="supplier">A supplier for any referenced maps.</param>
        /// <returns>The parsed source map.</returns>
        public static SourceMapping Parse(string contents, ISourceMapSupplier supplier)
        {
            // Version 1, starts with a magic string
            if (contents.StartsWith("/** Begin line maps. **/")) 
            {
                throw new SourceMapParseException(
                    "This appears to be a V1 SourceMap, which is not supported.");
            } 
            else if (contents.StartsWith("{"))
            {
                try 
                {
                    // Revision 2 and 3, are JSON Objects
                    JsonObject sourceMapRoot = new JsonObject(contents);
                    // Check basic assertions about the format.
                    int version = sourceMapRoot.getInt("version");
                    switch (version) 
                    {
                        case 3: 
                        {
                            SourceMapConsumerV3 consumer =  new SourceMapConsumerV3();
                            consumer.parse(sourceMapRoot, supplier);
                            return consumer;
                        }
                        default:
                            throw new SourceMapParseException(
                                "Unknown source map version:" + version);
                    }
                } 
                catch (Exception ex) 
                {
                    throw new SourceMapParseException("JSON parse exception: " + ex);
                }
            }
            throw new SourceMapParseException("unable to detect source map format");
        }
    }
}
