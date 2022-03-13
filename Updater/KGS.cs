#IMPORT System.Linq.dll
#IMPORT System.Core.dll
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NekoNyan {
    public class KGS {
        string[] Script;
		Encoding Eco = Encoding.UTF8;
		bool BOOM = false;
        public KGS(byte[] Script) {
			if (Script[0] == 0xFF && Script[1] == 0xFE)
			{
				BOOM = true;
				byte[] narr = new byte[Script.Length-2];
				for (int i = 2; i < Script.Length; i++)
					narr[i-2] = Script[i];
				this.Script = Eco.GetString(narr).Replace("\r\n", "\n").Split('\n');
				return;
			}
            this.Script = Eco.GetString(Script).Replace("\r\n", "\n").Split('\n');
        }

        public string[] Import() {
			List<string> Lines = new List<string>();
			
			for (int i = 0; i < Script.Length; i++){
				string line = Script[i];
				
				if (line.Trim() == string.Empty || line.StartsWith("//") || line.StartsWith("_") || line.StartsWith("@") || line.StartsWith("#"))
					continue;
				
				int JapCount = 0;
				foreach (var Char in line){
					var Range = UnicodeRanges.GetRange(Char);
					
					if (SJISRange.Contains(Range))
						JapCount++;
				}
				
				int NonJap = line.Length - JapCount;
				if (JapCount > NonJap)
					continue;
				
				Lines.Add(line);
				Next:;
			}
			
            return Lines.ToArray();
        }
		
		List<Range> SJISRange = new List<Range>() {
			Range.Katakana,
			Range.Hiragana,
			Range.KatakanaPhoneticExtensions,
			Range.CJKUnifiedIdeographs,
			Range.GeneralPunctuation,
			Range.CJKSymbolsAndPunctuation,
			Range.HalfwidthAndFullwidthForms,
			//Range.BoxDrawing
		};
		
		public void AppendArray<T>(ref T[] Array, T Value){
			T[] Output = new T[Array.Length+1];
			Array.CopyTo(Output, 0);
			Output[Array.Length] = Value;
			Array = Output;
		}

        public byte[] Export(string[] Text) {
			StringBuilder SB = new StringBuilder();
			
			for (int i = 0, x = 0; i < Script.Length; i++){
				string line = Script[i];			
				
				if (line.Trim() == string.Empty || line.StartsWith("//") || line.StartsWith("_") || line.StartsWith("@") || line.StartsWith("#")){
					SB.AppendLine(line);
					continue;
				}
				
				
				int JapCount = 0;
				foreach (var Char in line){
					var Range = UnicodeRanges.GetRange(Char);
					
					if (SJISRange.Contains(Range))
						JapCount++;
				}
				
				int NonJap = line.Length - JapCount;
				if (JapCount > NonJap) {
					SB.AppendLine(line);
					continue;
				}
				
				SB.AppendLine(Text[x++]);
			}
			
			return Eco.GetBytes(SB.ToString());
        }

    }
	
	public static class UnicodeRanges {

        static UnicodeCategory[] Unprintable = new UnicodeCategory[] {
            UnicodeCategory.Control,
            UnicodeCategory.OtherNotAssigned,
            UnicodeCategory.Surrogate
        };

        public static bool IsUnprintable(ushort Char)  => Unprintable.Contains(char.GetUnicodeCategory((char)Char));
		
		static UnicodeRange[] _Ranges = null;
        static UnicodeRange[] Ranges => _Ranges ??= GetRanges();
        public static Range GetRange(ushort Char) {
            foreach (var CRange in Ranges) {
                if (Char >= CRange.Min && Char <= CRange.Max)
                    return CRange.Name;
            }

            return Range.Unk;
        }
        private static UnicodeRange[] GetRanges() {
            object[] List = new object[] {
                0x0020, 0x007F, Range.BasicLatin,
                0x2580, 0x259F, Range.BlockElements,
                0x00A0, 0x00FF, Range.Latin1Supplement,
                0x25A0, 0x25FF, Range.GeometricShapes,
                0x0100, 0x017F, Range.LatinExtendedA,
                0x2600, 0x26FF, Range.MiscellaneousSymbols,
                0x0180, 0x024F, Range.LatinExtendedB,
                0x2700, 0x27BF, Range.Dingbats,
                0x0250, 0x02AF, Range.IPAExtensions,
                0x27C0, 0x27EF, Range.MiscellaneousMathematicalSymbolsA,
                0x02B0, 0x02FF, Range.SpacingModifierLetters,
                0x27F0, 0x27FF, Range.SupplementalArrowsA,
                0x0300, 0x036F, Range.CombiningDiacriticalMarks,
                0x2800, 0x28FF, Range.BraillePatterns,
                0x0370, 0x03FF, Range.GreekandCoptic,
                0x2900, 0x297F, Range.SupplementalArrowsB,
                0x0400, 0x04FF, Range.Cyrillic,
                0x2980, 0x29FF, Range.MiscellaneousMathematicalSymbolsB,
                0x0500, 0x052F, Range.CyrillicSupplementary,
                0x2A00, 0x2AFF, Range.SupplementalMathematicalOperators,
                0x0530, 0x058F, Range.Armenian,
                0x2B00, 0x2BFF, Range.MiscellaneousSymbolsAndArrows,
                0x0590, 0x05FF, Range.Hebrew,
                0x2E80, 0x2EFF, Range.CJKRadicalsSupplement,
                0x0600, 0x06FF, Range.Arabic,
                0x2F00, 0x2FDF, Range.KangxiRadicals,
                0x0700, 0x074F, Range.Syriac,
                0x2FF0, 0x2FFF, Range.IdeographicDescriptionCharacters,
                0x0780, 0x07BF, Range.Thaana,
                0x3000, 0x303F, Range.CJKSymbolsAndPunctuation,
                0x0900, 0x097F, Range.Devanagari,
                0x3040, 0x309F, Range.Hiragana,
                0x0980, 0x09FF, Range.Bengali,
                0x30A0, 0x30FF, Range.Katakana,
                0x0A00, 0x0A7F, Range.Gurmukhi,
                0x3100, 0x312F, Range.Bopomofo,
                0x0A80, 0x0AFF, Range.Gujarati,
                0x3130, 0x318F, Range.HangulCompatibilityJamo,
                0x0B00, 0x0B7F, Range.Oriya,
                0x3190, 0x319F, Range.Kanbun,
                0x0B80, 0x0BFF, Range.Tamil,
                0x31A0, 0x31BF, Range.BopomofoExtended,
                0x0C00, 0x0C7F, Range.Telugu,
                0x31F0, 0x31FF, Range.KatakanaPhoneticExtensions,
                0x0C80, 0x0CFF, Range.Kannada,
                0x3200, 0x32FF, Range.EnclosedCJKLettersAndMonths,
                0x0D00, 0x0D7F, Range.Malayalam,
                0x3300, 0x33FF, Range.CJKCompatibility,
                0x0D80, 0x0DFF, Range.Sinhala,
                0x3400, 0x4DBF, Range.CJKUnifiedIdeographsExtensionA,
                0x0E00, 0x0E7F, Range.Thai,
                0x4DC0, 0x4DFF, Range.YijingHexagramSymbols,
                0x0E80, 0x0EFF, Range.Lao,
                0x4E00, 0x9FFF, Range.CJKUnifiedIdeographs,
                0x0F00, 0x0FFF, Range.Tibetan,
                0xA000, 0xA48F, Range.YiSyllables,
                0x1000, 0x109F, Range.Myanmar,
                0xA490, 0xA4CF, Range.YiRadicals,
                0x10A0, 0x10FF, Range.Georgian,
                0xAC00, 0xD7AF, Range.HangulSyllables,
                0x1100, 0x11FF, Range.HangulJamo,
                0xD800, 0xDB7F, Range.HighSurrogates,
                0x1200, 0x137F, Range.Ethiopic,
                0xDB80, 0xDBFF, Range.HighPrivateUseSurrogates,
                0x13A0, 0x13FF, Range.Cherokee,
                0xDC00, 0xDFFF, Range.LowSurrogates,
                0x1400, 0x167F, Range.UnifiedCanadianAboriginalSyllabics,
                0xE000, 0xF8FF, Range.PrivateUseArea,
                0x1680, 0x169F, Range.Ogham,
                0xF900, 0xFAFF, Range.CJKCompatibilityIdeographs,
                0x16A0, 0x16FF, Range.Runic,
                0xFB00, 0xFB4F, Range.AlphabeticPresentationForms,
                0x1700, 0x171F, Range.Tagalog,
                0xFB50, 0xFDFF, Range.ArabicPresentationFormsA,
                0x1720, 0x173F, Range.Hanunoo,
                0xFE00, 0xFE0F, Range.VariationSelectors,
                0x1740, 0x175F, Range.Buhid,
                0xFE20, 0xFE2F, Range.CombiningHalfMarks,
                0x1760, 0x177F, Range.Tagbanwa,
                0xFE30, 0xFE4F, Range.CJKCompatibilityForms,
                0x1780, 0x17FF, Range.Khmer,
                0xFE50, 0xFE6F, Range.SmallFormVariants,
                0x1800, 0x18AF, Range.Mongolian,
                0xFE70, 0xFEFF, Range.ArabicPresentationFormsB,
                0x1900, 0x194F, Range.Limbu,
                0xFF00, 0xFFEF, Range.HalfwidthAndFullwidthForms,
                0x1950, 0x197F, Range.TaiLe,
                0xFFF0, 0xFFFF, Range.Specials,
                0x19E0,  0x19FF,  Range.KhmerSymbols,
                0x10000, 0x1007F, Range.LinearBSyllabary,
                0x1D00,  0x1D7F,  Range.PhoneticExtensions,
                0x10080, 0x100FF, Range.LinearBIdeograms,
                0x1E00,  0x1EFF,  Range.LatinExtendedAdditional,
                0x10100, 0x1013F, Range.AegeanNumbers,
                0x1F00,  0x1FFF,  Range.GreekExtended,
                0x10300, 0x1032F, Range.OldItalic,
                0x2000,  0x206F,  Range.GeneralPunctuation,
                0x10330, 0x1034F, Range.Gothic,
                0x2070,  0x209F,  Range.SuperscriptsandSubscripts,
                0x10380, 0x1039F, Range.Ugaritic,
                0x20A0,  0x20CF,  Range.CurrencySymbols,
                0x10400, 0x1044F, Range.Deseret,
                0x20D0,  0x20FF,  Range.CombiningDiacriticalMarksforSymbols,
                0x10450, 0x1047F, Range.Shavian,
                0x2100,  0x214F,  Range.LetterlikeSymbols,
                0x10480, 0x104AF, Range.Osmanya,
                0x2150,  0x218F,  Range.NumberForms,
                0x10800, 0x1083F, Range.CypriotSyllabary,
                0x2190,  0x21FF,  Range.Arrows,
                0x1D000, 0x1D0FF, Range.ByzantineMusicalSymbols,
                0x2200,  0x22FF,  Range.MathematicalOperators,
                0x1D100, 0x1D1FF, Range.MusicalSymbols,
                0x2300,  0x23FF,  Range.MiscellaneousTechnical,
                0x1D300, 0x1D35F, Range.TaiXuanJingSymbols,
                0x2400,  0x243F,  Range.ControlPictures,
                0x1D400, 0x1D7FF, Range.MathematicalAlphanumericSymbols,
                0x2440,  0x245F,  Range.OpticalCharacterRecognition,
                0x20000, 0x2A6DF, Range.CJKUnifiedIdeographsExtensionB,
                0x2460,  0x24FF,  Range.EnclosedAlphanumerics,
                0x2F800, 0x2FA1F, Range.CJKCompatibilityIdeographsSupplement,
                0x2500,  0x257F,  Range.BoxDrawing,
                0xE0000, 0xE007F, Range.Tags
            };

            List<UnicodeRange> Ranges = new List<UnicodeRange>();
            for (int i = 0; i < List.Length / 3; i++) {
                UnicodeRange URange = new UnicodeRange() {
                    Min = (ushort)(int)List[(i * 3) + 0],
                    Max = (ushort)(int)List[(i * 3) + 1],
                    Name = (Range)List[(i * 3) + 2]
                };

                Ranges.Add(URange);
            }

            return Ranges.ToArray();
        }
    }

    struct UnicodeRange {
        public ushort Min;
        public ushort Max;

        public Range Name;
    }

    public enum Range {
        BasicLatin,
        BlockElements,
        Latin1Supplement,
        GeometricShapes,
        LatinExtendedA,
        MiscellaneousSymbols,
        LatinExtendedB,
        Dingbats,
        IPAExtensions,
        MiscellaneousMathematicalSymbolsA,
        SpacingModifierLetters,
        SupplementalArrowsA,
        CombiningDiacriticalMarks,
        BraillePatterns,
        GreekandCoptic,
        SupplementalArrowsB,
        Cyrillic,
        MiscellaneousMathematicalSymbolsB,
        CyrillicSupplementary,
        SupplementalMathematicalOperators,
        Armenian,
        MiscellaneousSymbolsAndArrows,
        Hebrew,
        CJKRadicalsSupplement,
        Arabic,
        KangxiRadicals,
        Syriac,
        IdeographicDescriptionCharacters,
        Thaana,
        CJKSymbolsAndPunctuation,
        Devanagari,
        Hiragana,
        Bengali,
        Katakana,
        Gurmukhi,
        Bopomofo,
        Gujarati,
        HangulCompatibilityJamo,
        Oriya,
        Kanbun,
        Tamil,
        BopomofoExtended,
        Telugu,
        KatakanaPhoneticExtensions,
        Kannada,
        EnclosedCJKLettersAndMonths,
        Malayalam,
        CJKCompatibility,
        Sinhala,
        CJKUnifiedIdeographsExtensionA,
        Thai,
        YijingHexagramSymbols,
        Lao,
        CJKUnifiedIdeographs,
        Tibetan,
        YiSyllables,
        Myanmar,
        YiRadicals,
        Georgian,
        HangulSyllables,
        HangulJamo,
        HighSurrogates,
        Ethiopic,
        HighPrivateUseSurrogates,
        Cherokee,
        LowSurrogates,
        UnifiedCanadianAboriginalSyllabics,
        PrivateUseArea,
        Ogham,
        CJKCompatibilityIdeographs,
        Runic,
        AlphabeticPresentationForms,
        Tagalog,
        ArabicPresentationFormsA,
        Hanunoo,
        VariationSelectors,
        Buhid,
        CombiningHalfMarks,
        Tagbanwa,
        CJKCompatibilityForms,
        Khmer,
        SmallFormVariants,
        Mongolian,
        ArabicPresentationFormsB,
        Limbu,
        HalfwidthAndFullwidthForms,
        TaiLe,
        Specials,
        KhmerSymbols,
        LinearBSyllabary,
        PhoneticExtensions,
        LinearBIdeograms,
        LatinExtendedAdditional,
        AegeanNumbers,
        GreekExtended,
        OldItalic,
        GeneralPunctuation,
        Gothic,
        SuperscriptsandSubscripts,
        Ugaritic,
        CurrencySymbols,
        Deseret,
        CombiningDiacriticalMarksforSymbols,
        Shavian,
        LetterlikeSymbols,
        Osmanya,
        NumberForms,
        CypriotSyllabary,
        Arrows,
        ByzantineMusicalSymbols,
        MathematicalOperators,
        MusicalSymbols,
        MiscellaneousTechnical,
        TaiXuanJingSymbols,
        ControlPictures,
        MathematicalAlphanumericSymbols,
        OpticalCharacterRecognition,
        CJKUnifiedIdeographsExtensionB,
        EnclosedAlphanumerics,
        CJKCompatibilityIdeographsSupplement,
        BoxDrawing,
        Tags,
        Unk
    }
}


