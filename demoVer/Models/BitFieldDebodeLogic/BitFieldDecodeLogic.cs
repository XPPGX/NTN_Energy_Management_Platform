using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class BitFieldDefinition
    {
        public string Name { get; set; } = string.Empty;
        public int StartBit { get; set; }
        public int Length { get; set; } = 1;
        public Dictionary<int, string>? Values { get; set; }
    }

    public class CommandBitDefinition
    {
        public List<BitFieldDefinition> Fields { get; set; } = new();

        [JsonIgnore]
        public Dictionary<int, BitFieldDefinition> BitIndexLookup { get; } = new();

        public void BuildLookup()
        {
            BitIndexLookup.Clear();

            foreach (var field in Fields)
            {
                if (field.Length <= 0)
                {
                    continue;
                }

                BitIndexLookup[field.StartBit] = field;
            }
        }

        public bool TryGetFieldByBit(int bitIndex, out BitFieldDefinition? field)
        {
            if (BitIndexLookup.Count == 0)
            {
                BuildLookup();
            }

            return BitIndexLookup.TryGetValue(bitIndex, out field);
        }
    }

    public class CommandBitFieldSpec
    {
        public Dictionary<string, CommandBitDefinition> BitDefinitions { get; set; } = new();

        public void BuildLookups()
        {
            foreach (var definition in BitDefinitions.Values)
            {
                definition.BuildLookup();
            }
        }

        public bool TryGetField(string cmdName, int bitIndex, out BitFieldDefinition? field)
        {
            field = null;

            if (!BitDefinitions.TryGetValue(cmdName, out var definition))
            {
                return false;
            }

            return definition.TryGetFieldByBit(bitIndex, out field);
        }
    }
}