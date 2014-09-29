/*
 * Copyright 2009 The Closure Compiler Authors.
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

namespace ClosureSourceMaps
{
    // @author johnlenz@google.com (John Lenz)
    /// <summary>
    /// A list of currently support SourceMap format revisions.
    /// </summary>
    public enum SourceMapFormat
    {
        // The latest "stable" format
        Default,
        // V3: A nice compact format
        V3
    }

    // @author johnlenz@google.com (John Lenz)
    public static class SourceMapGeneratorFactory
    {
        /// <summary></summary>
        /// <returns>The appropriate source map object for the given source map format.</returns>
        public static ISourceMapGenerator GetInstance()
        {
            return GetInstance(SourceMapFormat.Default);
        }

        /// <summary></summary>
        /// <param name="format"></param>
        /// <returns>The appropriate source map object for the given source map format.</returns>
        public static ISourceMapGenerator GetInstance(SourceMapFormat format)
        {
            switch (format)
            {
                case SourceMapFormat.Default:
                case SourceMapFormat.V3:
                    return new SourceMapGeneratorV3();
                default:
                    throw new NotSupportedException("unsupported source map format");
            }
        }
    }
}
