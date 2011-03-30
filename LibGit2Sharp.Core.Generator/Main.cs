/*
 * The MIT License
 *
 * Copyright (c) 2011 Andrius Bentkus
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;

namespace LibGit2Sharp.Core.Generator
{
    public static class XmlExtensions
    {
        public static string Get(this XmlNode node, string attribute)
        {
            return node.Attributes[attribute].Value;
        }
    
        public static string Id(this XmlNode node)
        {
            return node.Get("id");
        }
    
        public static string Name(this XmlNode node)
        {
            return node.Get("name");
        }
    
        public static string Type(this XmlNode node)
        {
            return node.Get("type");
        }
    
        public static string Returns(this XmlNode node)
        {
            return node.Get("returns");
        }
    }
    
    public interface INet
    {
        string GetNetString();
        string GetNetString(bool returnType);
    }
    
    public class BaseType : INet
    {
        public string Object { get; set; }
        public int Pointer { get; set; }
        public bool Const { get; set; }
        public bool ReturnType { get; set; }
        
        public string PointerString
        {
            get {
                string star = "";

                for (int i = 0; i < Pointer; i++)
                    star += '*';
    
                return star;
            }
        }
        
        public override string ToString()
        {
            if (Pointer > 0)
                return string.Format("{0} {1}", Object, PointerString);
            else
                return string.Format("{0} ", Object);
        }

        public string GetNetString(bool returnType)
        {
            string obj = Object;
    
            switch (obj)
            {
                case "long long int":
                    return "long ";
                case "unsigned char":
                    if (Pointer == 1)
                        return "string ";
                    break;
                case "unsigned int":
                    obj = "uint";
                    break;
                case "long int":
                    return "long ";
                case "char":
                    if (Pointer == 1)
                        if (returnType ? !Const : Const)
                        	return "string ";
                        else
                        	return "sbyte *";
                    break;
            }
    
            Object = obj;
            return ToString();
        }
    
        public string GetNetString()
        {
            return GetNetString(false);
        }
    }

    public class Function : INet
    {
        public class Argument : INet
        {
            public string Name       { get; set; }
            public BaseType BaseType { get; set; }
    
            public override string ToString ()
            {
                return BaseType + Name;
            }
    
            public string GetNetString(bool returnType)
            {
                string name = Name;
                switch (name)
                {
                    case "object":
                        name = "obj";
                        break;
                    case "out":
                        name = "outt";
                        break;
                    case "ref":
                        name = "reference";
                        break;
                }
    
                return BaseType.GetNetString(returnType) + name;
            }

            public string GetNetString()
            {
                return GetNetString(false);
            }
        }

        public string Name          { get; set; }
        public Argument[] Arguments { get; set; }
        public BaseType  ReturnType { get; set; }
    
        public string ArgumentsString
        {
            get {
                return Arguments.Select(s => s.ToString()).ToArray().Join(", ");
            }
        }
        
        public override string ToString ()
        {
            return string.Format("{0} {1}{2}({3})", ReturnType.Object,
                                                    ReturnType.PointerString,
                                                    Name,
                                                    ArgumentsString);
        }

        public string ArgumentsNetString
        {
            get {
                return Arguments.Select(s => s.GetNetString()).ToArray().Join(", ");
            }
        }
    
        public string GetNetString(bool returnType)
        {
            return string.Format("{0}{1}({2})", ReturnType.GetNetString(returnType),
                                                Name,
                                                ArgumentsNetString);
        }

        public string GetNetString()
        {
            return GetNetString(true);
        }
    }

    public class GCCXmlParser
    {
        private Dictionary<string, XmlNode> ids = new Dictionary<string, XmlNode>();
    
        private XmlDocument xmldoc;
    
        public GCCXmlParser(XmlDocument xmldoc)
        {
            this.xmldoc = xmldoc;
    
            this.ParseIds();
        }
    
        private void ParseIds()
        {
            var gccxml = xmldoc.SelectSingleNode("/GCC_XML");
    
            foreach (XmlNode node in gccxml.ChildNodes)
            {
                ids[node.Id()] = node;
            }
        }
    
        private XmlNode GetById(string id)
        {
            if (ids.ContainsKey(id))
                return ids[id];
    
            return null;
        }
    
        private BaseType ResolveType(string id)
        {
            BaseType bt = new BaseType();
            ResolveType(bt, id);
            return bt;
        }
        
        private void ResolveType(BaseType basetype, string id)
        {

            XmlNode type = GetById(id);
            switch (type.Name)
            {
                case "PointerType":
                    basetype.Pointer++;
                    ResolveType(basetype, type.Type());
                    break;
                case "Typedef":
                    ResolveType(basetype, type.Type());
                    break;
                case "CvQualifiedType":
                    basetype.Const = true;
                    ResolveType(basetype, type.Type());
                    break;
                case "FundamentalType":
                    basetype.Object = type.Name();
                    break;
                case "Enumeration":
                    basetype.Object = type.Name();
                    break;
                case "Struct":
                    basetype.Object = type.Name();
                    break;
                default:
                    throw new Exception();
            }
        }
    
        private Function ParseFunction(XmlNode node)
        {
            Function func = new Function();
    
            func.Name = node.Name();
    
            func.ReturnType = ResolveType(node.Returns());
    
            List<Function.Argument> args = new List<Function.Argument>();
    
            foreach (XmlNode attribute in node.ChildNodes)
            {
                if (attribute.Name == "Ellipsis")
                    continue;
    
                args.Add(new Function.Argument() {
                    Name = attribute.Name(),
                    BaseType = ResolveType(attribute.Type())
                });
            }
    
            func.Arguments = args.ToArray();
    
            return func;
        }
    
        public Function ParseFunction(string name)
        {
            var list = xmldoc.SelectNodes("/GCC_XML/Function");
            foreach (XmlNode node in list) {
                if (node.Name() == name)
                    return ParseFunction(node);
            }

            return null;
        }
    
        public void ParseFunctions()
        {
            var list = xmldoc.SelectNodes("/GCC_XML/Function");
            foreach (XmlNode node in list) {
                ParseFunction(node);
            }
        }
    }

    public class GitHeaderParser
    {
        private static Regex regex1 = new Regex(@"GIT_EXTERN\(.+\) (.+)\(.+\);", RegexOptions.Compiled);
        private static Regex regex2 = new Regex(@"GIT_EXTERN\(.+\) (.+)\(", RegexOptions.Compiled);
    
        public static List<string> ParseFile(Stream stream)
        {
            StreamReader sr = new StreamReader(stream);
    
            List<string> list = new List<string>();
    
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                Match match = regex1.Match(line);
                if (match.Success)
                {
                    list.Add(match.Groups[1].Value);
                }
                else
                {
                    match = regex2.Match(line);
                    if (match.Success)
                    {
                        list.Add(match.Groups[1].Value);
                    }
                }
            }
    
            return list;
        }
        
        public static List<string> ParseExternFunctions(string directory)
        {
            List<string> list = new List<string>();
    
            DirectoryInfo di = new DirectoryInfo(directory);
    
            foreach (var file in di.GetFiles("*.h"))
            {
                FileStream fs = File.Open(file.FullName, FileMode.Open);
                list.AddRange(ParseFile(fs));
                fs.Close();
            }

            return list;
        }
    }
    
    class MainClass
    {
        public static string License
        {
            get {
                StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("license.txt"));
                string ret = sr.ReadToEnd();
                sr.Close();
                return ret;
            }
        }

        private static string SourceDirectory = "../../libgit2/include/git2/";
        private static string XmlOutput       = "../../Resources/libgit2.xml";
    
        public static void Main(string[] args)
        {
            var functions = GitHeaderParser.ParseExternFunctions(SourceDirectory);
    
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(XmlOutput);
    
            GCCXmlParser p = new GCCXmlParser(xmldoc);
    
    
            TextWriter tw = null;
    
            if (args.Length > 0)
                tw = new StreamWriter(File.Open(args[0], FileMode.Create));
            else
                tw = System.Console.Out;
            
            tw.WriteLine(License);
            tw.WriteLine();
            tw.WriteLine("// This code is autogenerated, do not modify");
            tw.WriteLine();
            tw.WriteLine("using System;");
            tw.WriteLine("using System.Runtime.InteropServices;");
            tw.WriteLine();
            tw.WriteLine("namespace LibGit2Sharp.Core");
            tw.WriteLine("{");
            tw.WriteLine("    unsafe internal class NativeMethods");
            tw.WriteLine("    {");
            tw.WriteLine("        private const string libgit2 = \"git2.dll\";");
            tw.WriteLine();

            foreach (string function in functions)
            {
                var func = p.ParseFunction(function);
                if (func != null)
                    Function(tw, func);
            }

            tw.WriteLine("    }");
            tw.WriteLine("}");

            tw.Close();
        }

        public static void Function(TextWriter tw, Function func)
        {
            tw.WriteLine();
            tw.WriteLine("        [DllImport(libgit2)]");
            tw.Write("        public static extern ");
            tw.Write(func.GetNetString());
            tw.WriteLine(";");
        }
    }
}
