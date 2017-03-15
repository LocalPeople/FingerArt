using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.JScript.Vsa;
using Microsoft.JScript;
using System.CodeDom.Compiler;

namespace ControlLibraryUtils
{
    /// <summary>
    /// 公式配置读取类
    /// </summary>
    internal class ReplaceUtil
    {
        // JScript引擎
        private static VsaEngine Engine = VsaEngine.CreateEngine();
        private static StringBuilder sbForParserText = new StringBuilder();

        public static Dictionary<string, string> GetReplaceDictionary(List<string> keys, List<string> values, string formulaPath)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            AddRange(result, keys, values);
            AddRangeByFormula(result, formulaPath);
            return result;
        }

        private static void AddRangeByFormula(Dictionary<string, string> target, string path)
        {
            using (StreamReader sr = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read), Encoding.UTF8, false))
            {
                string resultString = string.Empty;
                while ((resultString = sr.ReadLine()) != null)
                {
                    if (resultString.StartsWith("##") || string.IsNullOrWhiteSpace(resultString))
                        continue;
                    string[] keyValue = resultString.Split(' ');
                    string evalString;
                    if (FormulaParser(target, keyValue[1], out evalString))
                    {
                        object jScriptObject = Eval.JScriptEvaluate(evalString, Engine);
                        if (jScriptObject is string || jScriptObject is int)
                            target.Add(keyValue[0], jScriptObject.ToString());
                        else
                            target.Add(keyValue[0], Math.Round((double)jScriptObject, 3).ToString());
                    }
                    else
                        target.Add(keyValue[0], keyValue[1]);
                }
            }
        }

        public static bool FormulaParser(Dictionary<string, string> parser, string oldString, out string parseString)
        {
            string[] pieces = oldString.Split('$');
            if (pieces.Length >= 3)
            {
                sbForParserText.Clear();
                for (int i = 0; i < pieces.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        sbForParserText.Append(pieces[i]);
                    }
                    else
                    {
                        if (parser.ContainsKey(pieces[i]))
                            sbForParserText.Append(parser[pieces[i]]);
                        else
                            sbForParserText.Append("----" + pieces[i] + "----");
                    }
                }
                parseString = sbForParserText.ToString();
                return true;
            }
            parseString = oldString;
            return false;
        }

        private static void AddRange(Dictionary<string, string> target, List<string> keys, List<string> values)
        {
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                target.Add(keys[i], values[i].ToString());
            }
        }
    }
}
