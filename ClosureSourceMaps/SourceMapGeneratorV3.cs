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

// package com.google.debugging.sourcemap;
// import com.google.common.base.Preconditions;
// import com.google.common.collect.Lists;
// import com.google.common.collect.Maps;
// import com.google.debugging.sourcemap.SourceMapConsumerV3.EntryVisitor;

// import org.json.JSONObject;

// import java.io.IOException;
// import java.util.ArrayDeque;
// import java.util.Deque;
// import java.util.LinkedHashMap;
// import java.util.List;
// import java.util.Map;
// import java.util.Map.Entry;

// import javax.annotation.Nullable;

/**
 * Collects information mapping the generated (compiled) source back to
 * its original source for debugging purposes.
 *
 * Source Map Revision 3 Proposal:
 * https://docs.google.com/document/d/1U1RGAehQwRypUTovF1KRlpiOFze0b-_2gc6fAH0KY0k/edit?usp=sharing
 *
 * @author johnlenz@google.com (John Lenz)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClosureSourceMaps
{
    public static class Preconditions
    {
        public Preconditions() {}
        public static void CheckState(bool expression)
        {
            if (!expression)
                throw new Exception();
            
            return;
        }
        public static void CheckState(bool expression, string errorMessage)
        {

        }
    }

    class SourceMapGeneratorV3 : ISourceMapGenerator
    {
        /// <summary>
        /// This interface provides the merging strategy when an extension conflict
        /// appears because of merging two source maps on method 
        /// {@link #mergeMapSection}.
        /// </summary>
        public interface IExtensionMergeAction
        {

            /// <summary>
            /// Returns the merged value between two extensions with the same name when
            /// merging two source map.
            /// </summary>
            /// <param name="extensionKey">The extension name in conflict</param>
            /// <param name="currentValue">The extension value in the current source map</param>
            /// <param name="newValue">The extension value in the input source map</param>
            /// <returns>The merged value</returns>
            object Merge(string extensionKey, object currentValue, object newValue);
        }

        private static const int Unmapped = -1;

        /// <summary>
        /// A pre-order traversal ordered list of mappings stored in this map.
        /// </summary>
        private List<Mapping> mappings = new List<Mapping>();

        /// <summary>
        /// A map of source names to source name index.
        /// </summary>
        private Dictionary<string, int> sourceFileMap = new Dictionary<string,int>();

        /// <summary>
        /// A map of source names to source name index.
        /// </summary>
        private Dictionary<string, int> originalNameMap = new Dictionary<string,int>();

        /// <summary>
        /// Cache of the last mappings source name.
        /// </summary>
        private string lastSourceFile = null;

        /// <summary>
        /// Cache of the last mappings source name index.
        /// </summary>
        private int lastSourceFileIndex = -1;

        /// <summary>
        /// For validation store the last mapping added.
        /// </summary>
        private Mapping lastMapping;

        /// <summary>
        /// The position that the current source map is offset in the
        /// buffer being used to generated the compiled source file.
        /// </summary>
        private FilePosition offsetPosition = new FilePosition(0, 0);

        /// <summary>
        /// The position that the current source map is offset in the
        /// generated the compiled source file by the addition of a
        /// an output wrapper prefix.
        /// </summary>
        private FilePosition prefixPosition = new FilePosition(0, 0);

        /// <summary>
        /// A list of extensions to be added to sourcemap. The value is a object
        /// to permit single values, like strings or numbers, and JSONObject or
        /// JsonArray objects.
        /// </summary>
        private Dictionary<string, object> extensions = new Dictionary<string,object>();

        /// <summary>
        /// The source root path for relocating source fails or avoid duplicate values
        /// on the source entry.
        /// </summary>
        private string sourceRootPath;

        /**
        * {@inheritDoc}
        */
        public override void Reset()
        {
            mappings.Clear();
            lastMapping = null;
            sourceFileMap.Clear();
            originalNameMap.Clear();
            lastSourceFile = null;
            lastSourceFileIndex = -1;
            offsetPosition = new FilePosition(0, 0);
            prefixPosition = new FilePosition(0, 0);
        }

        /// <summary></summary>
        /// <param name="validate">Whether to perform (potentially costly) validation on the
        /// generated source map</param>  
        public override void Validate(bool validate) 
        {
            // Nothing currently.
        }

        /// <summary>
        /// Sets the prefix used for wrapping the generated source file before
        /// it is written. This ensures that the source map is adjusted for the
        /// change in character offsets.
        /// </summary>
        /// <param name="prefix">The prefix that is added before the generated source code.</param>
        public override void SetWrapperPrefix(string prefix)
        {
            // Determine the current line and character position.
            int prefixLine = 0;
            int prefixIndex = 0;

            for (int i = 0; i < prefix.Length; ++i) 
            {
                if (prefix[i] == '\n')
                {
                    ++prefixLine;
                    prefixIndex = 0;
                } 
                else 
                {
                    ++prefixIndex;
                }
            }

            prefixPosition = new FilePosition(prefixLine, prefixIndex);
        } 

        /// <summary>
        /// Sets the source code that exists in the buffer for which the
        /// generated code is being generated. This ensures that the source map
        /// accurately reflects the fact that the source is being appended to
        /// an existing buffer and as such, does not start at line 0, position 0
        /// but rather some other line and position.
        /// </summary>
        /// <param name="offsetLine">The index of the current line being printed.</param>
        /// <param name="offsetIndex">The column index of the current character being printed.</param>
        public override void SetStartingPosition(int offsetLine, int offsetIndex)
        {
            Preconditions.CheckState(offsetLine >= 0);
            Preconditions.CheckState(offsetIndex >= 0);
            offsetPosition = new FilePosition(offsetLine, offsetIndex);
        }

        /// <summary>
        /// Adds a mapping for the given node.  Mappings must be added in order.
        /// </summary>
        /// <param name="startPosition">The position on the starting line.</param>
        /// <param name="endPosition">The position on the ending line.</param>

        public override void AddMapping(string sourceName, string? symbolName,
                                        FilePosition sourceStartPosition, 
                                        FilePosition startPosition, FilePosition endPosition)
        {
            // Don't bother if there is not sufficient information to be useful.
            if (sourceName == null || sourceStartPosition.Line < 0) 
            {
                return;
            }

            FilePosition adjustedStart = startPosition;
            FilePosition adjustedEnd = endPosition;

            if (offsetPosition.Line != 0 || offsetPosition.Column != 0)
            {
                // If the mapping is found on the first line, we need to offset
                // its character position by the number of characters found on
                // the *last* line of the source file to which the code is
                // being generated.
                int offsetLine = offsetPosition.Line;
                int startOffsetPosition = offsetPosition.Column;
                int endOffsetPosition = offsetPosition.Column;

                if (startPosition.Line > 0) 
                {
                    startOffsetPosition = 0;
                }

                if (endPosition.Line > 0)
                {
                    endOffsetPosition = 0;
                }

                adjustedStart = new FilePosition(startPosition.Line + offsetLine,
                                                 startPosition.Column + startOffsetPosition);

                adjustedEnd = new FilePosition(endPosition.Line + offsetLine,
                                               endPosition.Column + endOffsetPosition);
            }

            // Create the new mapping.
            Mapping mapping = new Mapping();
            mapping.sourceFile = sourceName;
            mapping.originalPosition = sourceStartPosition;
            mapping.originalName = symbolName.Value;
            mapping.startPosition = adjustedStart;
            mapping.endPosition = adjustedEnd;

            // Validate the mappings are in a proper order.
            if (lastMapping != null) 
            {
                int lastLine = lastMapping.startPosition.Line;
                int lastColumn = lastMapping.startPosition.Column;
                int nextLine = mapping.startPosition.Line;
                int nextColumn = mapping.startPosition.Column;

                Preconditions.CheckState(nextLine > lastLine || (nextLine == lastLine && nextColumn >= lastColumn),
                                         string.Format("Incorrect source mappings order, previous : ({0},{1})\n"
                                         + "new : ({2},{3})\nnode : %s",
                                         lastLine, lastColumn, nextLine, nextColumn));
            }

            lastMapping = mapping;
            mappings.Add(mapping);
        }

        class ConsumerEntryVisitor: EntryVisitor 
        {
            public override void Visit(string sourceName, string symbolName, FilePosition sourceStartPosition,
                                       FilePosition startPosition, FilePosition endPosition) 
            {
                AddMapping(sourceName, symbolName, sourceStartPosition, startPosition, endPosition);
            }
        }

        /// <summary>
        /// Merges current mapping with {@code mapSectionContents} considering the
        /// offset {@code (line, column)}. Any extension in the map section will be
        /// ignored.
        /// </summary>
        /// <param name="line">The line offset.</param>
        /// <param name="column">The column offset.</param>
        /// <param name="mapSectionContents">The map section to be appended.</param>
        public void MergeMapSection(int line, int column, string mapSectionContents)
        {
            SetStartingPosition(line, column);
            SourceMapConsumerV3 section = new SourceMapConsumerV3();
            section.Parse(mapSectionContents);
            section.VisitMappings(new ConsumerEntryVisitor());
        }

        /// <summary>
        /// Works like {@link #mergeMapSection(int, int, String)}, except that
        /// extensions from the @{code mapSectionContents} are merged to the top level
        /// source map. For conflicts a {@code mergeAction} is performed.
        /// </summary>
        /// <param name="line">The line offset.</param>
        /// <param name="column">The column offset.</param>
        /// <param name="mapSectionContents">The map section to be appended.</param>
        /// <param name="mergeAction">The merge action for conflicting extensions.</param>
        public void MergeMapSection(int line, int column, string mapSectionContents, ExtensionMergeAction mergeAction)
        {
            SetStartingPosition(line, column);
            SourceMapConsumerV3 section = new SourceMapConsumerV3();
            section.Parse(mapSectionContents);
            section.VisitMappings(new ConsumerEntryVisitor());
            for (Entry<string, object> entry : section.getExtensions().entrySet())
            {
                string extensionKey = entry.getKey();
                if (extensions.ContainsKey(extensionKey)) 
                {
                    extensions.Add(extensionKey, mergeAction.merge(extensionKey, extensions[extensionKey],
                                   entry.getValue()));
                }
                else 
                {
                    extensions.Add(extensionKey, entry.getValue());
                }
            }
        }     

        /// <summary>
        /// Writes out the source map in the following format (line numbers are for
        /// reference only and are not part of the format):
        /// 
        /// 1.  {
        /// 2.    version: 3,
        /// 3.    file: "out.js",
        /// 4.    lineCount: 2,
        /// 5.    sourceRoot: "",
        /// 6.    sources: ["foo.js", "bar.js"],
        /// 7.    names: ["src", "maps", "are", "fun"],
        /// 8.    mappings: "a;;abcde,abcd,a;"
        /// 9.    x_org_extension: value
        /// 10. }
        ///
        /// Line 1: The entire file is a single JSON object
        /// Line 2: File revision (always the first entry in the object)
        /// Line 3: The name of the file that this source map is associated with.
        /// Line 4: The number of lines represented in the source map.
        /// Line 5: An optional source root, useful for relocating source files on a
        ///         server or removing repeated prefix values in the "sources" entry.
        /// Line 6: A list of sources used by the "mappings" entry relative to the
        ///         sourceRoot.
        /// Line 7: A list of symbol names used by the "mapping" entry.  This list
        ///         may be incomplete.
        /// Line 8: The mappings field.
        /// Line 9: Any custom field (extension).
        /// </summary>
        /// <param name="output"></param>
        /// <param name="name"></param>
        public override void AppendTo(Appendable output, string name)
        {
            int maxLine = prepMappings();

            // Add the header fields.
            output.append("{\n");
            appendFirstField(output, "version", "3");
            appendField(output, "file", escapeString(name));
            appendField(output, "lineCount", String.valueOf(maxLine + 1));

            //optional source root
            if (this.sourceRootPath != null && !this.sourceRootPath.isEmpty()) 
            {
                appendField(output, "sourceRoot", escapeString(this.sourceRootPath));
            }

            // Add the mappings themselves.
            appendFieldStart(output, "mappings");
            // out.append("[");
            (new LineMapper(output)).appendLineMappings();
            // out.append("]");
            appendFieldEnd(output);

            // Files names
            appendFieldStart(output, "sources");
            output.append("[");
            addSourceNameMap(output);
            output.append("]");
            appendFieldEnd(output);

            // Files names
            appendFieldStart(output, "names");
            output.append("[");
            addSymbolNameMap(output);
            output.append("]");
            appendFieldEnd(output);

            // Extensions, only if there is any
            for (String key : this.extensions.keySet())
            {
                Object objValue = this.extensions[key];
                String value = new String(objValue.toString());
                if (objValue instanceof String)
                {
                    value = JsonObject.quote(value);
                }
                appendField(output, key, value);
            }

            output.append("\n}\n");
        }

        /// <summary>
        /// A prefix to be added to the beginning of each sourceName passed to
        /// {@link #addMapping}. Debuggers expect (prefix + sourceName) to be a Url
        /// for loading the source code.
        /// </summary>
        /// <param name="path">The URL prefix to save in the sourcemap file. (Not validated)</param>
        public void SetSourceRoot(string path)
        {
            this.sourceRootPath = path;
        }
     
        /// <summary>
        /// Adds field extensions to the json source map. The value is allowed to be
        /// any value accepted by json, eg. string, JSONObject, JSONArray, etc.
        ///
        /// {@link org.json.JSONObject#put(String, Object)}
        ///
        /// Extensions must follow the format x_orgranization_field (based on V3
        /// proposal), otherwise a {@code SourceMapParseExtension} will be thrown.
        /// </summary>
        /// <param name="name">The name of the extension with format organization_field.</param>
        /// <param name="obj">The value of the extension as a valid json value.</param>
        public void AddExtension(string name, object obj)
        {
            if (!name.StartsWith("x_"))
            {
                throw new SourceMapParseException("Extension '" + name +
                                        "' must start with 'x_'");
            }
            this.extensions.Add(name, obj);
        }
        
        /// <summary>
        /// Removes an extension by name if present.
        /// </summary>
        /// <param name="name">The name of the extension with format organization_field.</param>
        public void RemoveExtension(string name)
        {
            if (this.extensions.ContainsKey(name))
            {
                this.extensions.Remove(name);
            }
        }

        /// <summary>
        /// Check whether or not the sourcemap has an extension.
        /// </summary>
        /// <param name="name">The name of the extension with format organization_field.</param>
        /// <returns>If the extension exist.</returns>
        public bool HasExtension(string name)
        {
            return this.extensions.ContainsKey(name);
        }

        /// <summary>
        /// Returns the value mapped by the specified extension
        /// or {@code null} if this extension does not exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The extension value or {@code null}.</returns>
        public object GetExtension(string name)
        {
            return this.extensions[name];
        }

        /// <summary>
        /// Writes the source name map to 'output'.
        /// </summary>
        /// <param name="output"></param>
        private void addSourceNameMap(Appendable output)
        {
            addNameMap(output, sourceFileMap);
        }

        /// <summary>
        /// Writes the source name map to 'output'.
        /// </summary>
        /// <param name="output"></param>
        private void addSymbolNameMap(Appendable output)
        {
            addNameMap(output, originalNameMap);
        }

        private void addNameMap(Appendable output, Map<String, Integer> map)
        {
            int i = 0;
            for (Entry<String, Integer> entry : map.entrySet())
            {
                string key = entry.getKey();
                if (i != 0)
                {
                    output.append(",");
                }
                output.append(escapeString(key));
                ++i;
            }
        }

        /// <summary>
        /// Escapes the given string for Json.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string escapeString(string value)
        {
            return Util.escapeString(value);
        }

        // Source map field helpers.

        private static void appendFirstField(Appendable output, string name, CharSequence value)
        {
            output.append("\"");
            output.append(name);
            output.append("\"");
            output.append(":");
            output.append(value);
        }

        private static void appendField(Appendable output, string name, CharSequence value)
        {
            output.append(",\n");
            output.append("\"");
            output.append(name);
            output.append("\"");
            output.append(":");
            output.append(value);
        }

        private static void appendFieldStart(Appendable output, string name)
        {
            appendField(output, name, "");
        }

        @SuppressWarnings("unused")
        private static void appendFieldEnd(Appendable output)
        {
        }

        /// <summary>
        /// Assigns sequential ids to used mappings, and returns the last line mapped.
        /// </summary>
        /// <returns></returns>
        private int prepMappings()
        {
            // Mark any unused mappings.
            (new MappingTraversal()).traverse(new UsedMappingCheck());

            // Renumber used mappings and keep track of the last line.
            int id = 0;
            int maxLine = 0;
            for (Mapping m : mappings)
            {
                if (m.used)
                {
                    m.id = id++;
                    int endPositionLine = m.endPosition.getLine();
                    maxLine = Math.Max(maxLine, endPositionLine);
                }
            }

            // Adjust for the prefix.
            return maxLine + prefixPosition.getLine();
        }

        /// <summary>
        /// A mapping from a given position in an input source file to a given position
        /// in the generated code.
        /// </summary>
        class Mapping 
        {
            /// <summary>
            /// A unique ID for this mapping for record keeping purposes.
            /// </summary>
            public int id = Unmapped;

            /// <summary>
            /// The source file index.
            /// </summary>
            public string sourceFile;

            /// <summary>
            /// The position of the code in the input source file. Both
            /// the line number and the character index are indexed by
            /// 1 for legacy reasons via the Rhino Node class.
            /// </summary>
            public FilePosition originalPosition;
            
            /// <summary>
            /// The starting position of the code in the generated source
            /// file which this mapping represents. Indexed by 0.
            /// </summary>
            public FilePosition startPosition;

            /// <summary>
            /// The ending position of the code in the generated source
            /// file which this mapping represents. Indexed by 0.
            /// </summary>
            public FilePosition endPosition;

            /// <summary>
            /// The original name of the token found at the position
            /// represented by this mapping (if any).
            /// </summary>
            public string originalName;

            /// <summary>
            /// Whether the mapping is actually used by the source map.
            /// </summary>
            public bool used = false;
        }

        /// <summary>
        /// Mark any visited mapping as "used".
        /// </summary>
        private class UsedMappingCheck: IMappingVisitor
        {
            public override void Visit(Mapping m, int line, int col, int nextLine, int nextCol)
            {
                if (m != null)
                {
                    m.used = true;
                }
            }
        }

        private interface IMappingVisitor
        {
    
            /// <param name="m">The mapping for the current code segment. null if the segment
            ///                 is unmapped.</param>
            /// <param name="line">The starting line for this code segment.</param>
            /// <param name="col">The starting column for this code segment.</param>
            /// <param name="endLine">The ending line</param>
            /// <param name="endCol">The ending column.</param>
            void Visit(Mapping m, int line, int col, int endLine, int endCol);
        }

        /// <summary>
        /// Walk the mappings and visit each segment of the mappings, unmapped
        /// segments are visited with a null mapping, unused mapping are not visited.
        /// </summary>
        private class MappingTraversal
        {
            // The last line and column written
            private int line;
            private int col;

            MappingTraversal() {}

            // Append the line mapping entries.
            void traverse(IMappingVisitor v)
            {
                // The mapping list is ordered as a pre-order traversal.  The mapping
                // positions give us enough information to rebuild the stack and this
                // allows the building of the source map in O(n) time.
                Deque<Mapping> stack = new ArrayDeque<Mapping>();
                for (Mapping m : mappings)
                {
                    // Find the closest ancestor of the current mapping:
                    // An overlapping mapping is an ancestor of the current mapping, any
                    // non-overlapping mappings are siblings (or cousins) and must be
                    // closed in the reverse order of when they encountered.
                    while (!stack.isEmpty() && !isOverlapped(stack.peek(), m))
                    {
                        Mapping previous = stack.pop();
                        maybeVisit(v, previous);
                    }

                    // Any gaps between the current line position and the start of the
                    // current mapping belong to the parent.
                    Mapping parent = stack.peek();
                    maybeVisitParent(v, parent, m);

                    stack.push(m);
                }

                // There are no more children to be had, simply close the remaining
                // mappings in the reverse order of when they encountered.
                while (!stack.isEmpty())
                {
                    Mapping m = stack.pop();
                    maybeVisit(v, m);
                }
            }

            /// <param name="p"></param>
            /// <returns>The line adjusted for the prefix position.</returns>
            private int getAdjustedLine(FilePosition p)
            {
                return p.Line + prefixPosition.Line;
            }

            /// <param name="p"></param>
            /// <returns>The column adjusted for the prefix position.</returns>
            private int getAdjustedCol(FilePosition p)
            {
                int rawLine = p.Line;
                int rawCol = p.Column;
                
                // Only the first line needs the character position adjusted.
                return (rawLine != 0) ? rawCol : rawCol + prefixPosition.Column;
            }

            /// <returns>Whether m1 ends before m2 starts.</returns>
            private bool isOverlapped(Mapping m1, Mapping m2)
            {
                // No need to use adjusted values here, relative positions are sufficient.
                int l1 = m1.endPosition.Line;
                int l2 = m2.startPosition.Line;
                int c1 = m1.endPosition.Column;
                int c2 = m2.startPosition.Column;

                return (l1 == l2 && c1 >= c2) || l1 > l2;
            }

            /// <summary>
            /// Write any needed entries from the current position to the end of the
            /// provided mapping.
            /// </summary>
            private void maybeVisit(IMappingVisitor v, Mapping m)
            {
                int nextLine = getAdjustedLine(m.endPosition);
                int nextCol = getAdjustedCol(m.endPosition);
            
                // If this anything remaining in this mapping beyond the
                // current line and column position, write it out now.
                if (line < nextLine || (line == nextLine && col < nextCol))
                {
                    visit(v, m, nextLine, nextCol);
                }
            }

            /// <summary>
            /// Write any needed entries to complete the provided mapping.
            /// </summary>
            private void maybeVisitParent(IMappingVisitor v, Mapping parent, Mapping m)
            {
                int nextLine = getAdjustedLine(m.startPosition);
                int nextCol = getAdjustedCol(m.startPosition);

                // If the previous value is null, no mapping exists.
                Preconditions.CheckState(line < nextLine || col <= nextCol);
                if (line < nextLine || (line == nextLine && col < nextCol))
                {
                    visit(v, parent, nextLine, nextCol);
                }
            }

            /// <summary>
            /// Write any entries needed between the current position the next position
            /// and update the current position.
            /// </summary>
            private void visit(IMappingVisitor v, Mapping m, int nextLine, int nextCol)
            {
                Preconditions.CheckState(line <= nextLine);
                Preconditions.CheckState(line < nextLine || col < nextCol);

                if (line == nextLine && col == nextCol)
                {
                    // Nothing to do.
                    Preconditions.CheckState(false);
                    return;
                }

                v.Visit(m, line, col, nextLine, nextCol);

                line = nextLine;
                col = nextCol;
            }
        }
        
        /// <summary>
        /// Appends the index source map to the given buffer.
        /// </summary>
        /// <param name="output">The stream to which the map will be appended.</param>
        /// <param name="name">The name of the generated source file that this source map
        ///  represents.</param>
        /// <param name="sections">An ordered list of map sections to include in the index.</param>
        public override void AppendIndexMapTo(Appendable output, string name, List<SourceMapSection> sections)
        {
            // Add the header fields.
            output.append("{\n");
            appendFirstField(output, "version", "3");
            appendField(output, "file", escapeString(name));

            // Add the line character maps.
            appendFieldStart(output, "sections");
            output.append("[\n");
            bool first = true;
            for (SourceMapSection section : sections)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    output.append(",\n");
                }
                output.append("{\n");
                appendFirstField(output, "offset",
                offsetValue(section.Line, section.Column));
                if (section.Type == SourceMapSection.SectionType.Url)
                {
                    appendField(output, "url", escapeString(section.Value));
                }
                else if (section.Type == SourceMapSection.SectionType.Map)
                {
                    appendField(output, "map", section.Value);
                }
                else
                {
                    throw new Exception("Unexpected section type");
                }
                output.append("\n}");
            }

            output.append("\n]");
            appendFieldEnd(output);

            output.append("\n}\n");
        }

        private CharSequence offsetValue(int line, int column)
        {
            StringBuilder output = new StringBuilder();
            output.append("{\n");
            appendFirstField(output, "line", String.valueOf(line));
            appendField(output, "column", String.valueOf(column));
            output.append("\n}");
            return output;
        }

        private int getSourceId(string sourceName)
        {
            if (sourceName != lastSourceFile)
            {
                lastSourceFile = sourceName;
                Integer index = sourceFileMap[sourceName];
                if (index != null)
                {
                    lastSourceFileIndex = index;
                }
                else
                {
                    lastSourceFileIndex = sourceFileMap.Count;
                    sourceFileMap.Add(sourceName, lastSourceFileIndex);
                }
            }
            return lastSourceFileIndex;
        }

        private int getNameId(string symbolName) 
        {
            int originalNameIndex;
            Integer index = originalNameMap[symbolName];
            if (index != null)
            {
                originalNameIndex = index;
            } 
            else 
            {
                originalNameIndex = originalNameMap.Count;
                originalNameMap.Add(symbolName, originalNameIndex);
            }
            return originalNameIndex;
        }

        private class LineMapper: IMappingVisitor 
        {
            // The destination.
            private readonly Appendable output;

            private int previousLine = -1;
            private int previousColumn = 0;

            // Previous values used for storing relative ids.
            private int previousSourceFileId;
            private int previousSourceLine;
            private int previousSourceColumn;
            private int previousNameId;

            LineMapper(Appendable output) 
            {
                this.output = output;
            }

            /// <summary>
            /// As each segment is visited write out the appropriate line mapping.
            /// </summary>
            public override void Visit(Mapping m, int line, int col, int nextLine, int nextCol)
            {
                if (previousLine != line)
                {
                    previousColumn = 0;
                }

                if (line != nextLine || col != nextCol) 
                {
                    if (previousLine == line) 
                    { 
                        // not the first entry for the line
                        output.append(',');
                    }
                    writeEntry(m, col);
                    previousLine = line;
                    previousColumn = col;
                }

                for (int i = line; i <= nextLine; i++) 
                {
                    if (i == nextLine) 
                    {
                        break;
                    }

                    closeLine(false);
                    openLine(false);
                }
            }

            /// <summary>
            /// Writes an entry for the given column (of the generated text) and
            /// associated mapping.
            /// The values are stored as relative to the last seen values for each
            /// field and encoded as Base64VLQs.
            /// </summary>
            void writeEntry(Mapping m, int column)
            {
                // The relative generated column number
                Base64Vlq.Encode(output, column - previousColumn);
                previousColumn = column;
                if (m != null) 
                {
                    // The relative source file id
                    int sourceId = getSourceId(m.sourceFile);
                    Base64Vlq.Encode(output, sourceId - previousSourceFileId);
                    previousSourceFileId = sourceId;

                    // The relative source file line and column
                    int srcline = m.originalPosition.Line;
                    int srcColumn = m.originalPosition.Column;
                    Base64Vlq.Encode(output, srcline - previousSourceLine);
                    previousSourceLine = srcline;

                    Base64Vlq.Encode(output, srcColumn - previousSourceColumn);
                    previousSourceColumn = srcColumn;

                    if (m.originalName != null) 
                    {
                        // The relative id for the associated symbol name
                        int nameId = getNameId(m.originalName);
                        Base64Vlq.Encode(output, (nameId - previousNameId));
                        previousNameId = nameId;
                    }
                }
            }

            // Append the line mapping entries.
            void appendLineMappings() 
            {
                // Start the first line.
                openLine(true);

                (new MappingTraversal()).traverse(this);

                // And close the final line.
                closeLine(true);
            }

            /// <summary>
            /// Begin the entry for a new line.
            /// </summary>
            private void openLine(bool firstEntry)
            {
                if (firstEntry) 
                {
                    output.append('\"');
                }
            }

            /// <summary>
            /// End the entry for a line.
            /// </summary>
            private void closeLine(bool finalEntry)
            {
                output.append(';');
                if (finalEntry) 
                {
                    output.append('\"');
                }
            }
        }
    }
}