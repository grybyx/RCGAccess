namespace RCGAccess
{
    /// <summary>Examples of the mod's sounds, played in the mod menu on the entries they belong to and in the tone test.
    /// The parameters are the same as where the sounds are really played.</summary>
    public static class Samples
    {
        private static float KV { get { return Settings.KnockVolume / 100f; } }

        /// <summary>Echo sounder: enemy off your line (low, slow, then faster), then on your line (high), weak enemy (quieter).</summary>
        public static void Knock(float d)
        {
            float v = KV;
            float[] at = { 0, 700, 1250, 1650, 1950, 2170 };
            foreach (float a in at) Speech.RawTone(Combat.LowPitch, 60, -0.6f, v, 0, Combat.LowPitch * 0.85f, d + a);
            for (int i = 0; i < 6; i++) Speech.RawTone(Combat.HighPitch, 45, -0.6f, v * 0.8f, 0, Combat.HighPitch, d + 2700 + i * 160);
            Speech.RawTone(Combat.LowPitch, 60, -0.6f, v * 0.3f, 0, Combat.LowPitch * 0.85f, d + 3900);
        }

        /// <summary>Same line: low tones, then the high tone when the enemy is on your line.</summary>
        public static void Line(float d)
        {
            float v = KV;
            Speech.RawTone(Combat.LowPitch, 60, 0.5f, v, 0, Combat.LowPitch * 0.85f, d);
            Speech.RawTone(Combat.LowPitch, 60, 0.5f, v, 0, Combat.LowPitch * 0.85f, d + 400);
            Speech.RawTone(Combat.HighPitch, 45, 0.5f, v * 0.8f, 0, Combat.HighPitch, d + 800);
            Speech.RawTone(Combat.HighPitch, 45, 0.5f, v * 0.8f, 0, Combat.HighPitch, d + 1100);
        }

        /// <summary>Obstacle in the way (furniture, crate, car), not a wall.</summary>
        public static void Obstacle(float d)
        {
            Speech.Tone(170, 45, 0.6f, 0.8f, 1, 150, d);
            Speech.Tone(170, 45, 0.6f, 0.8f, 1, 150, d + 120);
        }

        public static void Attack(float d)
        {
            Speech.Tone(900, 35, 0.6f, 0.7f, 2, 1500, d);
            Speech.Tone(900, 35, 0.6f, 0.7f, 2, 1500, d + 60);
        }

        /// <summary>Damage, healing, low health.</summary>
        public static void Health(float d)
        {
            Speech.Tone(180, 140, 0, 0.8f, 2, 120, d);
            Speech.Tone(500, 220, 0, 0.7f, 0, 900, d + 600);
            Speech.Tone(110, 160, 0, 0.8f, 0, 100, d + 1300);
            Speech.Tone(110, 160, 0, 0.6f, 0, 100, d + 1560);
        }

        /// <summary>Enemy defeated, fight area locked, fight area cleared.</summary>
        public static void Fight(float d)
        {
            Speech.Tone(784, 90, -0.5f, 0.8f, 1, 784, d);
            Speech.Tone(659, 90, -0.5f, 0.8f, 1, 659, d + 95);
            Speech.Tone(523, 180, -0.5f, 0.8f, 1, 523, d + 190);
            Speech.Tone(300, 120, 0, 0.7f, 2, 300, d + 900);
            Speech.Tone(300, 120, 0, 0.7f, 2, 300, d + 1060);
            Speech.Tone(523, 120, 0, 0.7f, 0, 523, d + 1700);
            Speech.Tone(784, 200, 0, 0.7f, 0, 1046, d + 1830);
        }

        public static void Wall(float d) { Speech.Tone(90, 160, 0.9f, 0.8f, 3, 90, d); }

        public static void Pit(float d) { Speech.Tone(300, 260, -0.7f, 0.8f, 0, 150, d); }

        public static void Toy(float d)
        {
            Speech.Tone(1400, 40, 0.5f, 0.6f, 0, 2100, d);
            Speech.Tone(1400, 40, 0.5f, 0.6f, 0, 2100, d + 90);
        }

        /// <summary>Interaction possible, then leaving the area.</summary>
        public static void Interact(float d)
        {
            Speech.Tone(660, 60, 0, 0.5f, 1, 990, d);
            Speech.Tone(880, 120, 0, 0.45f, 1, 440, d + 600);
        }

        public static void Money(float d)
        {
            Speech.Tone(1318, 50, 0, 0.45f, 0, 1318, d);
            Speech.Tone(1760, 70, 0, 0.45f, 0, 1760, d + 55);
        }

        public static void Pickup(float d)
        {
            Speech.Tone(880, 70, 0, 0.6f, 1, 880, d);
            Speech.Tone(1175, 70, 0, 0.6f, 1, 1175, d + 75);
            Speech.Tone(1760, 110, 0, 0.6f, 1, 1760, d + 150);
        }

        public static void ItemAppears(float d) { Speech.Tone(1046, 80, 0.5f, 0.6f, 0, 1568, d); }

        public static void Arrival(float d) { Speech.Tone(392, 250, 0, 0.7f, 0, 784, d); }

        /// <summary>Beacon to an exit on the right, then to the quest exit up on the left, then target reached.</summary>
        public static void Beacon(float d)
        {
            Speech.Tone(330, 70, 0.8f, 0.7f, 1, 330, d);
            Speech.Tone(330, 70, 0.8f, 0.7f, 1, 330, d + 400);
            Speech.Tone(495, 70, -0.8f, 0.45f, 2, 495, d + 900);
            Speech.Tone(495, 70, -0.8f, 0.45f, 2, 495, d + 1300);
            Arrival(d + 1900);
        }

        /// <summary>All sounds in a row (Shift+F6 and the mod menu entry "Listen to all sounds").</summary>
        public static void All()
        {
            Speech.Say(Loc.T("tonetest"), true);
            float d = 2600;
            Beacon(d); d += 2700;
            Knock(d); d += 4300;
            Attack(d); d += 700;
            Health(d); d += 2300;
            Fight(d); d += 2600;
            Pit(d); d += 700;
            Toy(d); d += 600;
            Wall(d); d += 700;
            Obstacle(d); d += 600;
            Interact(d); d += 1300;
            Pickup(d); d += 800;
            Money(d);
        }
    }
}
