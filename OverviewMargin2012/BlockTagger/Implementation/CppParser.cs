// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Extensions.BlockTagger.Implementation
{
    class CppParser : BraceParser
    {
        public CppParser()
        {
        }

        protected override BlockType FindType(CodeBlock parent, string statement)
        {
            if (BraceParser.ContainsWord(statement, "foreach") || BraceParser.ContainsWord(statement, "for") || BraceParser.ContainsWord(statement, "while") || BraceParser.ContainsWord(statement, "do"))
                return BlockType.Loop;
            else if (BraceParser.ContainsWord(statement, "if") || BraceParser.ContainsWord(statement, "else") || BraceParser.ContainsWord(statement, "switch"))
                return BlockType.Conditional;
            else if (BraceParser.ContainsWord(statement, "class") || BraceParser.ContainsWord(statement, "struct") || BraceParser.ContainsWord(statement, "interface"))
                return BlockType.Class;
            else if (BraceParser.ContainsWord(statement, "namespace"))
                return BlockType.Namespace;
            else if ((parent.Type == BlockType.Class) || (parent.Type == BlockType.Namespace) || (parent.Type == BlockType.Root))
                return BlockType.Method;
            else
                return BlockType.Unknown;
        }
    }
}
