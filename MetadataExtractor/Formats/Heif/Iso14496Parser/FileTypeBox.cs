﻿using System.Collections.Generic;
using System.Linq;
using MetadataExtractor.IO;

namespace MetadataExtractor.Formats.Heif.Iso14496Parser
{
    public class FileTypeBox : Box
    {
        public uint MajorBrand { get; }
        public uint MinorBrand { get; }
        public IList<uint> CompatibleBrands { get; }

        public string MajorBrandString => TypeStringConverter.ToTypeString(MajorBrand);
        public string MinorBrandString => TypeStringConverter.ToTypeString(MinorBrand);

        public IEnumerable<string> CompatibleBrandStrings =>
            CompatibleBrands.Select(TypeStringConverter.ToTypeString);

        public FileTypeBox(BoxLocation loc, SequentialReader sr) : base(loc)
        {
            MajorBrand = sr.GetUInt32();
            MinorBrand = sr.GetUInt32();
            var cBrands = new List<uint>();
            CompatibleBrands = cBrands;
            while (!loc.DoneReading(sr))
            {
                cBrands.Add(sr.GetUInt32());
            }
        }
    }
}
