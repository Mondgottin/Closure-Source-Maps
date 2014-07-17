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
using Newtonsoft.Json.Linq;
using System.Collections;
using Newtonsoft.Json;
using System.Diagnostics;

// import org.json.JSONArray;
// import org.json.JSONException;
// import org.json.JSONObject;

// import java.io.IOException;
// import java.util.ArrayList;
// import java.util.Arrays;
// import java.util.Collection;
// import java.util.Collections;
// import java.util.HashMap;
// import java.util.Map;

namespace ClosureSourceMaps
{
    // @author johnlenz@google.com (John Lenz)
    /// <summary>
    /// Class for parsing version 3 of the SourceMap format, as produced by the
    /// Closure Compiler, etc.
    /// http://code.google.com/p/closure-compiler/wiki/SourceMaps
    /// </summary>
    class SourceMapConsumerV3 : ISourceMapConsumer, ISourceMappingReversable
    {
        static const int Unmapped = -1;

        private string[] sources;
        private string[] names;
        private int lineCount;
        // Slots in the lines list will be null if the line does not have any entries.
        private List<List<IEntry>> lines = null;
        /// <summary>
        /// originalFile path ==> original line ==> target mappings.
        /// </summary>
        private Dictionary<string, Dictionary<Int32, IEnumerable<OriginalMapping>>> reverseSourceMapping;
        private string sourceRoot;
        #warning Dictionary is not the same as LinkedHashMap
        private Dictionary<string, object> extensions = new Dictionary<string,object>();

        public SourceMapConsumerV3() {}

        class DefaultSourceMapSupplier: ISourceMapSupplier
        {
            public string GetSourceMap(string url)
            {
                return null;
            }
        }

        /// <summary>
        /// Parses the given contents containing a source map.
        /// </summary>
        /// <param name="contents"></param>
        public void Parse(string contents)
        {
            Parse(contents, null);
        }
    
        /// <summary>
        /// Parses the given contents containing a source map.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="sectionSupplier"></param>
        public void Parse(string contents, ISourceMapSupplier sectionSupplier)
        {
            try 
            {
                JObject sourceMapRoot = new JObject(contents);
                Parse(sourceMapRoot, sectionSupplier);
            }
            catch (Exception ex) 
            {
                #warning Clarify an exception
                throw new SourceMapParseException("Json parse exception: " + ex);
            }
        }

        /// <summary>
        /// Parses the given contents containing a source map.
        /// </summary>
        /// <param name="sourceMapRoot"></param>
        public void Parse(JObject sourceMapRoot)
        {
            Parse(sourceMapRoot, null);
        }

        /// <summary>
        /// Parses the given contents containing a source map.
        /// </summary>
        /// <param name="sourceMapRoot"></param>
        /// <param name="sectionSupplier"></param>
        public void Parse(JObject sourceMapRoot, ISourceMapSupplier sectionSupplier)
        {
            try 
            {
                // Check basic assertions about the format.
                int version = JsonConvert.DeserializeObject(sourceMapRoot.GetValue("version"), typeof(int));
                if (version != 3) 
                {
                    throw new SourceMapParseException("Unknown version: " + version);
                }
                #warning Clarify isEmpty() function
                if (String.IsNullOrEmpty((sourceMapRoot["file"]).ToString()))
                {
                    throw new SourceMapParseException("File entry is empty");
                }

                if (sourceMapRoot["sections"] != null) 
                {
                    // Looks like a index map, try to parse it that way.
                    parseMetaMap(sourceMapRoot, sectionSupplier);
                    return;
                }

                lineCount = sourceMapRoot["lineCount"] == null ? (int) sourceMapRoot["lineCount"] : -1;
                string lineMap = sourceMapRoot["mappings"].ToString();

                sources = getJavaStringArray(sourceMapRoot.getJSONArray("sources"));
                names = getJavaStringArray(sourceMapRoot.getJSONArray("names"));

                if (lineCount >= 0)
                {
                    lines = new List<List<IEntry>>(lineCount);
                } 
                else 
                {
                    lines = new List<List<IEntry>>();
                }

                if (sourceMapRoot["sourceRoot"] != null)
                {
                    sourceRoot = sourceMapRoot["sourceRoot"].ToString();
                }

                foreach (object objkey in Lists.newArrayList(sourceMapRoot.keys())) 
                {
                    string key = (string) objkey;
                    if (key.StartsWith("x_"))
                    {
                        extensions.Add(key, sourceMapRoot[key]);
                    }
                }

                new MappingBuilder(lineMap).build();
            }
            catch(Exception ex)
            {
                throw new SourceMapParseException("JSON parse exception: " + ex);
            }
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceMapRoot"></param>
        /// <param name="sectionSupplier"></param>
        private void parseMetaMap(JObject sourceMapRoot, SourceMapSupplier sectionSupplier)
        {
            if (sectionSupplier == null) 
            {
                sectionSupplier = new DefaultSourceMapSupplier();
            }

            try 
            {
                // Check basic assertions about the format.
                int version = (int) sourceMapRoot["version"];
                if (version != 3)
                {
                    throw new SourceMapParseException("Unknown version: " + version);
                }

                string file = (string) sourceMapRoot["file"];
                if (String.IsNullOrEmpty(file))
                {
                    throw new SourceMapParseException("File entry is missing or empty");
                }

                if (sourceMapRoot.has("lineCount") || sourceMapRoot.has("mappings")
                   || sourceMapRoot.has("sources") || sourceMapRoot.has("names"))
                {
                    throw new SourceMapParseException("Invalid map format");
                }

                SourceMapGeneratorV3 generator = new SourceMapGeneratorV3();
                JSONArray sections = sourceMapRoot.getJSONArray("sections");
                for (int i = 0, count = sections.length(); i < count; i++) 
                {
                    JSONObject section = sections.getJSONObject(i);
                    if (section.has("map") && section.has("url"))
                    {
                        throw new SourceMapParseException("Invalid map format: section may not have both 'map' and 'url'");
                    }
                    JSONObject offset = section.getJSONObject("offset");
                    int line = offset.getInt("line");
                    int column = offset.getInt("column");
                    String mapSectionContents;
                    if (section.has("url")) 
                    {
                        String url = section.getString("url");
                        mapSectionContents = sectionSupplier.getSourceMap(url);
                        if (mapSectionContents == null) 
                        {
                            throw new SourceMapParseException("Unable to retrieve: " + url);
                        }
                    } 
                    else if (section.has("map")) 
                    {
                        mapSectionContents = section.getString("map");
                    } 
                    else 
                    {
                        throw new SourceMapParseException("Invalid map format: section must have either 'map' or 'url'");
                    }
                    generator.mergeMapSection(line, column, mapSectionContents);
                }

                StringBuilder sb = new StringBuilder();
                try 
                {
                    generator.AppendTo(sb, file);
                } 
                catch (IOException e) 
                {
                    // Can't happen.
                    throw new RuntimeException(e);
                }

                Parse(sb.ToString());
            }
            catch (IOException ex) 
            {
                throw new SourceMapParseException("IO exception: " + ex);
            } 
            catch (JSONException ex) 
            {
                throw new SourceMapParseException("JSON parse exception: " + ex);
            }
        }

        public override OriginalMapping getMappingForLine(int lineNumber, int column) 
        {
            // Normalize the line and column numbers to 0.
            --lineNumber;
            --column;

            if (lineNumber < 0 || lineNumber >= lines.Count) 
            {
                return null;
            }

            Trace.Assert(lineNumber >= 0);
            Trace.Assert(column >= 0);

            // If the line is empty return the previous mapping.
            if (lines[lineNumber] == null) 
            {
                return getPreviousMapping(lineNumber);
            }

            List<IEntry> entries = lines[lineNumber];
            // No empty lists.
            Trace.Assert(entries.Count > 0);
            if (entries[0].getGeneratedColumn() > column) 
            {
                return getPreviousMapping(lineNumber);
            }

            int index = search(entries, column, 0, entries.Count - 1);
            Trace.Assert(index >= 0, string.Format("unexpected:{0}s", index));
            return getOriginalMappingForEntry(entries[index]);
        }

        public override List<string> getOriginalSources() 
        {
            return sources.ToList<string>();
        }

        public override List<OriginalMapping> getReverseMapping(string originalFile, int line, int column) 
        {
            // TODO(user): This implementation currently does not make use of the column
            // parameter.

            // Synchronization needs to be handled by callers.
            if (reverseSourceMapping == null) 
            {
                createReverseMapping();
            }

            Dictionary<int, List<OriginalMapping>> sourceLineToCollectionMap =
            reverseSourceMapping.get(originalFile);

            if (sourceLineToCollectionMap == null) 
            {
                return new List<OriginalMapping>();
            } 
            else 
            {
                List<OriginalMapping> mappings = sourceLineToCollectionMap[line];

                if (mappings == null) 
                {
                    return new List<OriginalMapping>();
                } 
                else 
                {
                    return mappings;
                }
            }
        }

        public String getSourceRoot()
        {
            return this.sourceRoot;
        }

        /// <summary>
        /// Returns all extensions and their values (which can be any json value)
        /// in a Map object.
        /// </summary>
        /// <returns>The extension list.</returns>
        public Dictionary<string, object> getExtensions()
        {
            return this.extensions;
        }


        private String[] getJavaStringArray(JSONArray array)
        {
            int len = array.length();
            String[] result = new String[len];
            for(int i = 0; i < len; i++) 
            {
                result[i] = array.getString(i);
            }
            return result;
        }

        private class MappingBuilder 
        {
            private static const int MAX_ENTRY_VALUES = 5;
            private readonly StringCharIterator content;
            private int line = 0;
            private int previousCol = 0;
            private int previousSrcId = 0;
            private int previousSrcLine = 0;
            private int previousSrcColumn = 0;
            private int previousNameId = 0;

            public MappingBuilder(String lineMap) 
            {
                this.content = new StringCharIterator(lineMap);
            }

            public void build() 
            {
                int [] temp = new int[MAX_ENTRY_VALUES];
                List<IEntry> entries = new List<IEntry>();
                while (content.hasNext()) 
                {
                    // ';' denotes a new line.
                    if (tryConsumeToken(';')) 
                    {
                        // The line is complete, store the result for the line,
                        // null if the line is empty.
                        List<IEntry> result;
                        if (entries.Count > 0) 
                        {
                            result = entries;
                            // A new array list for the next line.
                            entries = new List<IEntry>();
                        } 
                        else 
                        {
                            result = null;
                        }
                        lines.Add(result);
                        entries.Clear();
                        line++;
                        previousCol = 0;
                    } 
                    else 
                    {
                        // grab the next entry for the current line.
                        int entryValues = 0;
                        while (!entryComplete()) 
                        {
                            temp[entryValues] = nextValue();
                            entryValues++;
                        }
                        IEntry entry = decodeEntry(temp, entryValues);

                        validateEntry(entry);
                        entries.Add(entry);

                      // Consume the separating token, if there is one.
                        tryConsumeToken(',');
                    }
                }
            }

            /// <summary>
            /// Sanity check the entry.
            /// </summary>
            /// <param name="entry"></param>
            private void validateEntry(IEntry entry) 
            {
                Trace.Assert((lineCount < 0) || (line < lineCount));
                Trace.Assert(entry.getSourceFileId() == Unmapped
                          || entry.getSourceFileId() < sources.Length);
                Trace.Assert(entry.getNameId() == Unmapped
                          || entry.getNameId() < names.Length);
            }

            /// <summary>
            /// Decodes the next entry, using the previous encountered values to
            /// decode the relative values.
            /// </summary>
            /// <param name="vals">An array of integers that represent values in the entry.</param>
            /// <param name="entryValues">The number of entries in the array.</param>
            /// <returns>The entry object.</returns>
            private IEntry decodeEntry(int[] vals, int entryValues) 
            {
                IEntry entry;
                switch (entryValues) 
                {
                    // The first values, if present are in the following order:
                    //   0: the starting column in the current line of the generated file
                    //   1: the id of the original source file
                    //   2: the starting line in the original source
                    //   3: the starting column in the original source
                    //   4: the id of the original symbol name
                    // The values are relative to the last encountered value for that field.
                    // Note: the previously column value for the generated file is reset
                    // to '0' when a new line is encountered.  This is done in the 'build'
                    // method.

                    case 1:
                        // An unmapped section of the generated file.
                        entry = new UnmappedEntry(vals[0] + previousCol);
                        // Set the values see for the next entry.
                        previousCol = entry.getGeneratedColumn();
                        return entry;

                    case 4:
                        // A mapped section of the generated file.
                        entry = new UnnamedEntry(vals[0] + previousCol,
                                                vals[1] + previousSrcId,
                                                vals[2] + previousSrcLine,
                                                vals[3] + previousSrcColumn);
                        // Set the values see for the next entry.
                        previousCol = entry.getGeneratedColumn();
                        previousSrcId = entry.getSourceFileId();
                        previousSrcLine = entry.getSourceLine();
                        previousSrcColumn = entry.getSourceColumn();
                        return entry;

                    case 5:
                        // A mapped section of the generated file, that has an associated
                        // name.
                        entry = new NamedEntry(vals[0] + previousCol,
                                              vals[1] + previousSrcId,
                                              vals[2] + previousSrcLine,
                                              vals[3] + previousSrcColumn,
                                              vals[4] + previousNameId);
                        // Set the values see for the next entry.
                        previousCol = entry.getGeneratedColumn();
                        previousSrcId = entry.getSourceFileId();
                        previousSrcLine = entry.getSourceLine();
                        previousSrcColumn = entry.getSourceColumn();
                        previousNameId = entry.getNameId();
                        return entry;

                    default:
                        throw new Exception("Unexpected number of values for entry:" + entryValues);
                }
            }

            private bool tryConsumeToken(char token) 
            {
                if (content.hasNext() && content.peek() == token) 
                {
                    // consume the comma
                    content.next();
                    return true;
                }
                return false;
            }

            private bool entryComplete() 
            {
                if (!content.hasNext()) 
                {
                    return true;
                }

                char c = content.peek();
                return (c == ';' || c == ',');
            }

            private int nextValue() 
            {
                return Base64Vlq.Decode(content);
            }
        }
        
        /// <summary>
        /// Perform a binary search on the array to find a section that covers
        /// the target column
        /// </summary>
        private int search(List<IEntry> entries, int target, int start, int end) 
        {
            while (true) 
            {
                int mid = ((end - start) / 2) + start;
                int compare = compareEntry(entries, mid, target);
                if (compare == 0) 
                {
                    return mid;
                }
                else if (compare < 0) 
                {
                    // it is in the upper half
                    start = mid + 1;
                    if (start > end) 
                    {
                        return end;
                    }
                } 
                else 
                {
                    // it is in the lower half
                    end = mid - 1;
                    if (end < start) 
                    {
                        return end;
                    }
                }
            }
        }

        /// <summary>
        /// Compare an array entry's column value to the target column value.
        /// </summary>
        private int compareEntry(List<IEntry> entries, int entry, int target) 
        {
            return entries[entry].getGeneratedColumn() - target;
        }
        
        /// <summary>
        /// Returns the mapping entry that proceeds the supplied line or null if no
        /// such entry exists.
        /// </summary>
        private OriginalMapping getPreviousMapping(int lineNumber) 
        {
            do 
            {
                if (lineNumber == 0) 
                {
                    return null;
                }
                lineNumber--;
            } 
            while (lines[lineNumber] == null);
            List<IEntry> entries = lines[lineNumber];
            return getOriginalMappingForEntry(entries[entries.Count - 1]);
        }

        /// <summary>
        /// Creates an "OriginalMapping" object for the given entry object.
        /// </summary>
        private OriginalMapping getOriginalMappingForEntry(IEntry entry) 
        {
            if (entry.getSourceFileId() == Unmapped) 
            {
                return null;
            } 
            else 
            {
                // Adjust the line/column here to be start at 1.
                Builder x = OriginalMapping.newBuilder()
                .setOriginalFile(sources[entry.getSourceFileId()])
                .setLineNumber(entry.getSourceLine() + 1)
                .setColumnPosition(entry.getSourceColumn() + 1);
                if (entry.getNameId() != Unmapped) 
                {
                    x.setIdentifier(names[entry.getNameId()]);
                }
                return x.build();
            }
        }

        /// <summary>
        /// Reverse the source map; the created mapping will allow us to quickly go
        /// from a source file and line number to a collection of target
        /// OriginalMappings.
        /// </summary>
        private void createReverseMapping() 
        {
            reverseSourceMapping =
                new Dictionary<string, Dictionary<int, List<OriginalMapping>>>();

            for (int targetLine = 0; targetLine < lines.Count; ++targetLine) 
            {
                List<IEntry> entries = lines[targetLine];

                if (entries != null) 
                {
                    foreach (IEntry entry in entries) 
                    {
                        if (entry.getSourceFileId() != Unmapped
                            && entry.getSourceLine() != Unmapped) 
                        {
                            string originalFile = sources[entry.getSourceFileId()];

                            if (!reverseSourceMapping.ContainsKey(originalFile)) 
                            {
                                #warning HashMap was used
                                reverseSourceMapping.Add(originalFile,
                                new Dictionary<int, List<OriginalMapping>>());
                            }

                            Dictionary<int, List<OriginalMapping>> lineToCollectionMap =
                                reverseSourceMapping.get(originalFile)  ;

                            int sourceLine = entry.getSourceLine();

                            if (!lineToCollectionMap.ContainsKey(sourceLine)) 
                            {
                                lineToCollectionMap.Add(sourceLine,
                                    new List<OriginalMapping>(1));
                            }

                            List<OriginalMapping> mappings =
                                lineToCollectionMap[sourceLine];

                            Builder builder = OriginalMapping.newBuilder().setLineNumber(
                                targetLine).setColumnPosition(entry.getGeneratedColumn());

                            mappings.Add(builder.build());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A implementation of the Base64VLQ CharIterator used for decoding the
        /// mappings encoded in the JSON string.
        /// </summary>
        private class StringCharIterator: IEnumerable<char> 
        {
            readonly string content;
            readonly int length;
            int current = 0;

            public StringCharIterator(string content) 
            {
                this.content = content;
                this.length = content.Length;
            }

            public override char next() 
            {
                return content[current++];
            }

            public char peek() 
            {
                return content[current];
            }

            public override bool hasNext() 
            {
                return current < length;
            }
        }

        /// <summary>
        /// Represents a mapping entry in the source map.
        /// </summary>
        private interface IEntry 
        {
            int getGeneratedColumn();
            int getSourceFileId();
            int getSourceLine();
            int getSourceColumn();
            int getNameId();
        }

        /// <summary>
        /// This class represents a portion of the generated file, that is not mapped
        /// to a section in the original source.
        /// </summary>
        private class UnmappedEntry: IEntry 
        {
            private readonly int column;

            public UnmappedEntry(int column) 
            {
                this.column = column;
            }

            public override int getGeneratedColumn() 
            {
                return column;
            }

            public override int getSourceFileId() 
            {
                return Unmapped;
            }

            public override int getSourceLine() 
            {
                return Unmapped;
            }

            public override int getSourceColumn() 
            {
                return Unmapped;
            }

            public override int getNameId() 
            {
                return Unmapped;
            }
        }

        /// <summary>
        /// This class represents a portion of the generated file, that is mapped
        /// to a section in the original source.
        /// </summary>
        private class UnnamedEntry: UnmappedEntry 
        {
            private readonly int srcFile;
            private readonly int srcLine;
            private readonly int srcColumn;

            public UnnamedEntry(int column, int srcFile, int srcLine, int srcColumn)
                : base(column)
            {
                this.srcFile = srcFile;
                this.srcLine = srcLine;
                this.srcColumn = srcColumn;
            }

            public override int getSourceFileId() 
            {
                return srcFile;
            }

            public override int getSourceLine() 
            {
                return srcLine;
            }

            public override int getSourceColumn() 
            {
                return srcColumn;
            }

            public override int getNameId() 
            {
                return Unmapped;
            }
        }

        /// <summary>
        /// This class represents a portion of the generated file, that is mapped
        /// to a section in the original source, and is associated with a name.
        /// </summary>
        private class NamedEntry: UnnamedEntry 
        {
            private readonly int name;

            public NamedEntry(int column, int srcFile, int srcLine, int srcColumn, int name)                 
                :base(column, srcFile, srcLine, srcColumn)

            {
                this.name = name;
            }

            public override int getNameId() 
            {
                return name;
            }
        }

        public static interface EntryVisitor 
        {
            void visit(String sourceName,
                   String symbolName,
                   FilePosition sourceStartPosition,
                   FilePosition startPosition,
                   FilePosition endPosition);
        }

        public void visitMappings(EntryVisitor visitor) 
        {
            bool pending = false;
            String sourceName = null;
            String symbolName = null;
            FilePosition sourceStartPosition = null;
            FilePosition startPosition = null;

            int lineCount = lines.Count;
            for (int i = 0; i < lineCount; ++i) 
            {
                List<IEntry> line = lines[i];
                if (line != null) 
                {
                    int entryCount = line.Count;
                    for (int j = 0; j < entryCount; ++j) 
                    {
                        IEntry entry = line[j];
                        if (pending) 
                        {
                            FilePosition endPosition = new FilePosition(
                                i, entry.getGeneratedColumn());
                            visitor.visit(
                                        sourceName,
                                        symbolName,
                                        sourceStartPosition,
                                        startPosition,
                                        endPosition);
                            pending = false;
                        }

                        if (entry.getSourceFileId() != Unmapped) 
                        {
                            pending = true;
                            sourceName = sources[entry.getSourceFileId()];
                            symbolName = (entry.getNameId() != Unmapped) ? names[entry.getNameId()] : null;
                            sourceStartPosition = new FilePosition(
                                                    entry.getSourceLine(), entry.getSourceColumn());
                            startPosition = new FilePosition(
                                                    i, entry.getGeneratedColumn());
                        }
                    }
                }
            }
        }
    }
}
