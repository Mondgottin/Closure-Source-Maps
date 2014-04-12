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

// import java.io.IOException;  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClosureSourceMaps
{
    /// <summary>
    /// A class for mapping source map names to the actual contents.
    /// Used when parsing index maps.
    /// 
    /// @author johnlenz@google.com (John Lenz)
    /// </summary>
    public interface ISourceMapSupplier
    {
        /// <summary>
        /// </summary>
        /// <param name="url">The URL of the source map.</param>
        /// <returns>The contents of the map associated with the URL.</returns>
        string GetSourceMap(string url);
    }
}
