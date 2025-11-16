namespace Route24.GameOff
{
    public static class ShipNames
    {
        public static string GetRandomShipName()
        {
            int index = UnityEngine.Random.Range(0, _shipNames.Length);
            return _shipNames[index];
        }
        
        private static readonly string[] _shipNames =
        {
            "The Horizon",
            "Sea Whisper",
            "Iron Whale",
            "The Trident",
            "Silent Tide",
            "Stormbreaker",
            "Wavecrest",
            "The Mariner",
            "Deep Voyager",
            "The Leviathan"
        };
    }
}