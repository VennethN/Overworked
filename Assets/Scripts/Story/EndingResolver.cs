using Overworked.Core;
using Overworked.Story.Data;

namespace Overworked.Story
{
    public static class EndingResolver
    {
        public const string ENDING_RESIGN = "resign";
        public const string ENDING_SECRET = "secret";
        public const string ENDING_BREAKDOWN = "breakdown";
        public const string ENDING_SURVIVE = "survive";

        // Evidence flags the player can collect
        private static readonly string[] EvidenceFlags = {
            "read_evidence_d6",
            "read_blacklist_d7",
            "read_burnout_d7",
            "dika_evidence_d6",
            "forwarded_evidence_d7"
        };

        public static string Resolve(SaveData save, StoryCollection storyData)
        {
            // 1. Resign ending — player confirmed resignation on day 6
            if (save.HasFlag("confirmed_resign_d6"))
                return ENDING_RESIGN;

            // 2. Secret ending — collected enough evidence and forwarded it
            if (save.HasFlag("forwarded_evidence_d7"))
            {
                int evidenceCount = 0;
                foreach (string flag in EvidenceFlags)
                {
                    if (save.HasFlag(flag)) evidenceCount++;
                }
                if (evidenceCount >= 3)
                    return ENDING_SECRET;
            }

            // 3. Breakdown ending — failed too many days
            int failedDays = 0;
            if (storyData?.days != null)
            {
                foreach (var d in storyData.days)
                {
                    if (d.dayNumber > 7) continue;
                    if (save.GetBestScore(d.dayNumber) < d.scoreGoal) failedDays++;
                }
            }
            if (failedDays >= 4)
                return ENDING_BREAKDOWN;

            // 4. Survive ending — made it through
            return ENDING_SURVIVE;
        }

        public static DialogueLine[] GetEpilogueDialogue(string endingType)
        {
            return endingType switch
            {
                ENDING_SURVIVE => new[]
                {
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Akhirnya... hari terakhir selesai." },
                    new DialogueLine { speaker = "Kamu", avatar = "player", text = "..." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "{PlayerName} berjalan keluar gedung PT Sleep Deprivation Studio. Ekspresinya kosong." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Kamu bertahan. Tapi... apakah itu berarti menang?" },
                },
                ENDING_BREAKDOWN => new[]
                {
                    new DialogueLine { speaker = "SYSTEM", avatar = "system", text = "Kamu masih bekerja." },
                    new DialogueLine { speaker = "SYSTEM", avatar = "system", text = "Tapi bukan kamu lagi yang mengendalikan." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Tubuhmu masih di sini. Duduk di kursi yang sama. Mengetik di keyboard yang sama." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Tapi matamu sudah kosong. Pikiranmu sudah pergi." },
                },
                ENDING_RESIGN => new[]
                {
                    new DialogueLine { speaker = "Kamu", avatar = "player", text = "Saya mau berhenti." },
                    new DialogueLine { speaker = "HR", avatar = "hr", text = "Tentu... kami tidak menahan siapapun." },
                    new DialogueLine { speaker = "HR", avatar = "hr", text = "Tapi industri ini kecil." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Lamaran kerja berikutnya: DITOLAK.\n\"Kami tidak dapat melanjutkan proses Anda.\"" },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Dan berikutnya. Dan berikutnya. Dan berikutnya." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Evaluasi negatif dari PT Sleep Deprivation Studio sudah menyebar ke seluruh industri." },
                },
                ENDING_SECRET => new[]
                {
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "{PlayerName} mengumpulkan semua bukti. Laporan internal. Data blacklist. Rekam jejak burnout." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Dan mengirimkannya ke pihak berwenang." },
                    new DialogueLine { speaker = "BREAKING NEWS", avatar = "system", text = "PT Sleep Deprivation Studio sedang diselidiki atas dugaan pelanggaran hak ketenagakerjaan." },
                    new DialogueLine { speaker = "BREAKING NEWS", avatar = "system", text = "Sejumlah petinggi perusahaan telah dipanggil untuk dimintai keterangan." },
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Untuk pertama kalinya... sistem yang mengawasi... diawasi balik." },
                },
                _ => new[]
                {
                    new DialogueLine { speaker = "Narasi", avatar = "system", text = "Ceritamu berakhir di sini." },
                }
            };
        }
    }
}
