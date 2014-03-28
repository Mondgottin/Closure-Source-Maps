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

namespace ClosureSourceMaps
{
    public class SourceMapSection
    {
        /// <summary>
        /// A Url for a valid source Map file that represents a section of a generate
        /// source file such as when multiple files are concatenated together.
        /// </summary>
        private readonly string value;
        private readonly int line;
        private readonly int column;
        private readonly SectionType type;
        public static enum SectionType
        {
            Url,
            Map
        }

        /// <summary></summary>
        /// <param name="sectionUrl">the Url for the partial source Map</param>
        /// <param name="line">the number of lines into the file where the represented section starts</param>
        /// <param name="column">the number of characters into the line where the represented
        /// section starts</param>
        [Obsolete()]
        public SourceMapSection(string sectionUrl, int line, int column) 
        {
            this.type = SectionType.Url;
            this.value = sectionUrl;
            this.line = line;
            this.column = column;
        }

        private SourceMapSection(SectionType type, string value, int line, int column) 
        {
            this.type = type;
            this.value = value;
            this.line = line;
            this.column = column;
        }

        public static SourceMapSection ForMap(string value, int line, int column) 
        {
            return new SourceMapSection(SectionType.Map, value, line, column);
        }

        public static SourceMapSection ForUrl(string value, int line, int column) 
        {
            return new SourceMapSection(SectionType.Url, value, line, column);
        }

        public SectionType Type 
        {
            get
            {
                return this.type;
            }
        }

        /// <summary>
        /// Return the name of the Map.
        /// </summary>
        [Obsolete()]
        public string SectionUrl
        {
            get
            {
                if (!type.Equals(SectionType.Url))
                    throw(new Exception());
                return value;
            }
        }

        /// <summary>
        /// Return the value that represents the Map for this section.
        /// </summary>
        public string Value
        {
            get
            {
                return value;
            }
        }

        /// <summary>
        /// Return the starting line for this section.
        /// </summary>
        public int Line
        {
            get
            {
                return line;
            }
        }

        /// <summary>
        /// Return the column for this section.
        /// </summary>
        public int Column
        {
            get
            {
                return column;
            }
        }
    }
}
