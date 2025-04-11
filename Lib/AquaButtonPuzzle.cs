using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BunchOfButtonsLib
{
    public class AquaButtonPuzzle
    {
        public string[] Clues { get; private set; }
        public string Word { get; private set; }

        private static bool[][] BrailleBits = @"1 12 14 145 15 124 1245 125 24 245 13 123 134 1345 135 1234 12345 1235 234 2345 136 1236 2456 1346 13456 1356"
            .Split(' ')
            .Select(braille => Enumerable.Range(0, 6).Select(i => braille.Contains((char)('1' + i))).ToArray())
            .ToArray();

        public static AquaButtonPuzzle GeneratePuzzle(int seed)
        {
            var rnd = new Random(seed);

            var word = WordLists.SixLetters[rnd.Next(0, WordLists.SixLetters.Length)];

            var bitmap = new bool[6 * 6];
            for (var i = 0; i < word.Length; i++)
                for (var dot = 0; dot < 6; dot++)
                    bitmap[2 * (i % 3) + (dot / 3) + 6 * (3 * (i / 3) + (dot % 3))] = BrailleBits[word[i] - 'A'][dot];

            var colClues = Enumerable.Range(0, 6).Select(col => Enumerable.Range(0, 6).Select(y => bitmap[col + 6 * y]).CreateNonogramClue().JoinString(" ")).Select(i => i.Length == 0 ? "0" : i).ToArray();
            var rowClues = Enumerable.Range(0, 6).Select(row => Enumerable.Range(0, 6).Select(x => bitmap[x + 6 * row]).CreateNonogramClue().JoinString(" ")).Select(i => i.Length == 0 ? "0" : i).Reverse().ToArray();

            return new AquaButtonPuzzle { Clues = rowClues.Concat(colClues).ToArray(), Word = word };
        }
    }
}
