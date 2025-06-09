using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UAssetAPI;
using UAssetAPI.JSON;
using UAssetAPI.UnrealTypes;

namespace Overdare
{
    internal class FPackageIndexUpdater
    {
        public JsonSerializer Serializer;

        public FPackageIndexUpdater(int?[] updateMap, UAsset asset, KillResult killResult)
        {
            Dictionary<FName, string> toBeFilled = new Dictionary<FName, string>();
            Serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new OverrideFPackageIndexResolver(toBeFilled, asset, updateMap, killResult),
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                FloatParseHandling = FloatParseHandling.Double,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                Converters = new List<JsonConverter>()
            {
                //new FSignedZeroJsonConverter(),
                //new FNameJsonConverter(null),
                //new FStringTableJsonConverter(),
                //new FStringJsonConverter(),
                new FPackageIndexJsonConverter(),
                //new StringEnumConverter(),
                //new GuidJsonConverter(),
                //new ByteArrayJsonConverter()
            },
                Error = (sender, args) =>
                {
                    // Skip the problematic member
                    //Console.WriteLine($"Skipping error: {args.ErrorContext.Error.Message}");
                    args.ErrorContext.Handled = true;
                }
            });
        }
    }

    internal class KillResult
    {
        public bool Value = false;
    }

    internal class CustomFPackageIndexJsonConverter : JsonConverter
    {
        public UAsset CurrentAsset;
        public int?[] UpdateMap;
        public KillResult KillResult;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FPackageIndex);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            //if (value is FPackageIndex yindex) Console.WriteLine(yindex.Index);
            if (value is FPackageIndex pindex && pindex.IsExport())
            {
                //Console.WriteLine("AAA");
                //Console.WriteLine(UpdateMap.Length);
                //Console.WriteLine(pindex.ToExport(CurrentAsset) == CurrentAsset.Exports[118]);
                //Console.WriteLine(pindex.Index - 1);
                //Console.WriteLine($"{pindex.Index - 1} is {UpdateMap[pindex.Index - 1] ?? 0}");
                //Console.WriteLine("AAA");
                var indexToKill = pindex.Index - 1;
                pindex.Index = UpdateMap[pindex.Index - 1] + 1 ?? 0;
                if (pindex.IsNull())
                {
                    Console.WriteLine($"Index {indexToKill} was killed now index: {pindex.Index}");
                    KillResult.Value = true;
                }
            }
            writer.WriteNull();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            //Console.WriteLine($"hacking fucking FPackageIndex :D {CurrentAsset.FilePath} {Convert.ToInt32(reader.Value)}");
            return new FPackageIndex(Convert.ToInt32(reader.Value));
        }

        public CustomFPackageIndexJsonConverter(UAsset asset, int?[] updateMap, KillResult killResult) : base()
        {
            CurrentAsset = asset;
            UpdateMap = updateMap;
            KillResult = killResult;
        }
    }

    internal class OverrideFPackageIndexResolver : DefaultContractResolver
    {
        public UAsset CurrentAsset;
        public int?[] UpdateMap;
        public KillResult KillResult;

        protected override JsonContract CreateContract(Type objectType)
        {
            JsonContract contract = base.CreateContract(objectType);
            if (objectType == typeof(FPackageIndex))
            {
                contract.Converter = new CustomFPackageIndexJsonConverter(CurrentAsset, UpdateMap, KillResult);
            }
            return contract;
        }

        public Dictionary<FName, string> ToBeFilled;

        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(FName).IsAssignableFrom(objectType))
            {
                return new FNameJsonConverter(ToBeFilled);
            }
            return base.ResolveContractConverter(objectType);
        }

        public OverrideFPackageIndexResolver(Dictionary<FName, string> toBeFilled, UAsset currentAsset, int?[] updateMap, KillResult killResult) : base()
        {
            ToBeFilled = toBeFilled;
            CurrentAsset = currentAsset;
            UpdateMap = updateMap;
            KillResult = killResult;
        }
    }
}
