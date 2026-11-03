using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace FilipinoFolkloreApp.Services
{
    public static class AlamatContent
    {
        public class Narrator
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Avatar { get; set; } = "";
            public int PriceStars { get; set; } = 0;
            public string NarratorBackground { get; set; } = "";
        }

        public class Slide
        {
            public string Background { get; set; } = "";
            public List<string> Characters { get; set; } = new();
            public string? Subtitle { get; set; }

            public Dictionary<string, string> AudioByNarrator { get; set; } = new();
        }

        public class QuizQuestion
        {
            public string Prompt { get; set; } = "";
            public List<string> ChoiceTexts { get; set; } = new(); 
            public int CorrectIndex { get; set; } = 0;
            public int TimeLimitSec { get; set; } = 1000;
            public int RewardStars { get; set; } = 10; 
        }

        public class Story
        {
            public int StoryIndex { get; set; } = 0;
            public string Id { get; set; } = ""; // monitor
            public string Title { get; set; } = "";
            public int MedalId { get; set; } = 0;
            public int PriceStars { get; set; } = 0;
            public string Category { get; set; } = "";
            public int RewardStars { get; set; } = 20;
            public string Thumb = "";
            public bool IsRewardClaimed = false; // monitor
            public bool IsPurchased = false; // monitor
            public bool NarratorEagleUnlocked = false; // monitor
            public bool NarratorMonkeyUnlocked = false; // monitor
            public List<Slide> Slides { get; set; } = new();
            public List<QuizQuestion> Quiz { get; set; } = new();
        }

        // ------- EDIT THESE: narrators, stories, slides, quiz -------
        public static List<Narrator> Narrators { get; } = new()
        {
            new Narrator{ Id="tarsier", Name="Tarsier", Avatar="elements/tarsier.png",NarratorBackground ="Ako si Tarsy maliit lang ako, pero may malalaking mata na nakakatulong sa akin na makita kahit madilim ang paligid. Mula pagkabata, ako na ang bantay ng gubat tuwing gabi. Ako ang nagbabantay para siguraduhing ligtas ang lahat. Dito sa laro, tutulungan kitang makita ang mga bagay na mahirap makita kapag gabi at turuan kang maging matapang kahit madilim ang paligid. Pinili ako dahil gusto kong ipakita na kahit maliit ka, may malaking bagay kang magagawa kapag matapang ka at mapagmasid sa iyong paligid. Tara, sasamahan kita na maglakbay sa gabi.", PriceStars=0 },
            new Narrator{ Id="eagle",   Name="Agila",   Avatar="elements/eagle.png",NarratorBackground="Ako si Agie isang agila na malakas ang pakpak at mabilis lumipad sa himpapawid. Nakikita ko ang buong mundo mula sa taas ng langit. Palagi akong nagmamasid kung ano ang nangyayari mula sa itaas, para maprotektahan ang mga naninirahan sa gubat sa baba. Pinili mo ako para maging iyong gabay dahil gusto kitang turuan na maging matapang at laging handang tumulong sa oras ng problema. Dito sa laro, tutulungan kitang tingnan ang mga bagay mula sa malayo at mapag matyag, para lagi kang handa sa kahit anong mangyayari. Sama na, lipad tayo at alamin ang mga sikreto ng gubat",   PriceStars=50 },
            new Narrator{ Id="monkey",  Name="Unggoy",  Avatar="elements/monkey.png",NarratorBackground ="Ako si Makkie ang unggoy na laging masaya at malikot. Mahilig ako tumalon-talon sa mga puno at maghanap ng masasarap na saging na paborito kong prutas. Palagi akong naglalaro sa gubat at nakikisama sa iba pang mga hayop. Pinili mo ako para maging narrator dahil gusto kong ipakita sa'yo na ang gubat ay puno ng saya at magkaron ng mga kaibigan. Dito sa laro, tutulungan kitang maghanap ng mga kayamanan at matuto habang ikaw ay naglalaro. Halika samahan mo ako sa masayang pakikipagsapalaran sa gubat.",  PriceStars=100 },
        };
        // Replace the async methods with synchronous versions:

        public static string[] LoadSubtitles(string fileName)
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync(fileName).GetAwaiter().GetResult();
                using var reader = new StreamReader(stream);

                var subtitles = new List<string>();
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Replace literal \n with actual newline character
                        line = line.Replace("\\n", "\n");
                        subtitles.Add(line);
                    }
                }

                return subtitles.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading subtitles from {fileName}: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static List<Slide> CreateSlidesFromSubtitles(string subtitleFile, string backgroundPrefix, string storyName)
        {
            var subtitles = LoadSubtitles(subtitleFile);
            var slides = new List<Slide>();

            for (int i = 0; i < subtitles.Length; i++)
            {
                slides.Add(new Slide
                {
                    Background = $"{backgroundPrefix}/{storyName}{i + 1}.png",
                    Subtitle = subtitles[i],
                    AudioByNarrator = new()
                    {
                        ["tarsier"] = $"{backgroundPrefix}_voices/tarsier/{storyName}{i + 1}_tarsier.m4a",
                        ["eagle"] = $"{backgroundPrefix}_voices/eagle/{storyName}{i + 1}_eagle.mp3",
                        ["monkey"] = $"{backgroundPrefix}_voices/monkey/{storyName}{i + 1}_monkey.m4a",
                    }
                });
            }

            return slides;
        }
        public static List<Story> Stories { get; } = new()
        {
            new Story{
                StoryIndex=1, Category = "alamat", MedalId = 1,
                Id="1_juan_tamad", Title="Alamat ng Alimango", PriceStars=0, RewardStars=50, 
                Thumb ="storiesalamat/alamatngalimgango/iconalamatngalimango.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/alamatngalimango.txt","storiesalamat/alamatngalimango","alamatngalimango"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino ang anak nina Mang Mando at Aling Idang?",
                        ChoiceTexts=new(){ "Alimango", "Aliman", "Aling Idang" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ugali ni Aliman?",
                        ChoiceTexts=new(){ "Mabait at matulungin", "Lagi siyang nananakit sa ibang bata", "Mahilig mag-aral" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ni Aliman sa batang natutulak?",
                        ChoiceTexts=new(){ "Tinulungan siya", "Itinulak siya nang malakas sa putikan", "Nilaro siya" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang nangyari kay Aliman habang naliligo sa dagat?",
                        ChoiceTexts=new(){ "Nagbago ang anyo ng kaniyang katawan at naging alimango", "Natutong lumangoy", "Nawala sa dagat" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Bakit tinawag siyang “Alimango”?",
                        ChoiceTexts=new(){ "Dahil mahilig siya sa dagat", "Dahil nagbago ang katawan niya at naging alimango", "Dahil may paborito siyang pagkain" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    }



                }
            },
            new Story{
                StoryIndex =2,Category = "alamat", MedalId = 2,
                Id="2_alamatngbahaghari", Title="alamatngbahaghari", PriceStars=20, RewardStars=50,Thumb="storiesalamat/alamatngbahaghari/iconalamatngbahaghari.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/alamatngbahaghari.txt","storiesalamat/alamatngbahaghari","alamatngbahaghari"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino-sino ang mga naninirahan sa langit?",
                        ChoiceTexts=new(){ "Pula, Kahel, Dilaw, Berde, Bughaw, Morado, Lila", "Pula, Kahel, Itim, Puti", "Pula, Dilaw, Asul, Itim" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang tungkulin ng mga kulay?",
                        ChoiceTexts=new(){ "Magbigay saya sa mundo", "Magturo sa mga tao", "Magluto ng pagkain" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Bakit nag-away ang mga kulay?",
                        ChoiceTexts=new(){ "Dahil gusto nilang maglaro", "Dahil may iba't ibang idea kung paano mapasaya ang tao", "Dahil gutom sila" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ng Panginoon sa kanila?",
                        ChoiceTexts=new(){ "Pinagsama-sama ang lahat ng kulay", "Pinadala sila sa lupa", "Pinaupo sila sa langit" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang resulta pagkatapos ng ulan?",
                        ChoiceTexts=new(){ "Nagkaroon ng bahaghari", "Nalunod ang mga kulay", "Nawala ang lahat ng kulay" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    }
                }
            },
            new Story{
                StoryIndex =3,Category = "alamat", MedalId = 3,
                Id="3_alamatngpinya", Title="Alamat ng Pinya", PriceStars=20, RewardStars=50,
                Thumb= "storiesalamat/alamatngpinya/iconalamatngpinya.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/alamatngpinya.txt","storiesalamat/alamatngpinya","alamatngpinya"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino ang anak ni Aling Rosa?",
                        ChoiceTexts=new(){ "Pinang", "Aliman", "Pinya" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ugali ni Pinang?",
                        ChoiceTexts=new(){ "Mabait", "Malikot", "Matulungin" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang nangyari sa lugaw ni Pinang?",
                        ChoiceTexts=new(){ "Nasunog", "Niluto ng maayos", "Nawala" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang sinabi ni Aling Rosa para turuan ang anak?",
                        ChoiceTexts=new(){ "Maraming mata", "Maraming kamay", "Magpakalakas" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang naging prutas ni Pinang?",
                        ChoiceTexts=new(){ "Mangga", "Pinya", "Saging" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    }
}
            },
            new Story{
                StoryIndex=4,Category = "alamat", MedalId= 4,
                Id="4_batmayarawbuwanatbituin", Title="Bat may araw,buwan at bituin", PriceStars=30, RewardStars=50,
                Thumb="storiesalamat/batmayarawbuwanatbituin/iconbatmayarawbuwanatbituin.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/batmayarawbuwanatbituin.txt","storiesalamat/batmayarawbuwanatbituin","batmayarawbuwanatbituin"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino ang lumikha ng lalaki at babae sa kwento?",
                        ChoiceTexts=new(){ "Langit", "Bathala", "Luntian" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang trabaho ng lalaki at babae?",
                        ChoiceTexts=new(){ "Nagtatanim at nagbabayo ng palay", "Nagluluto at naglalaro", "Naglilinis ng bahay" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Bakit humiling ang lalaki kay Bathala na itaas ang langit?",
                        ChoiceTexts=new(){ "Hindi niya maabot ang langit habang nagbabayong palay", "Gusto niyang makita ang buwan", "Nawala ang kanyang suklay" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang nangyari sa mga kuwintas at suklay ng babae?",
                        ChoiceTexts=new(){ "Nahulog sa lupa", "Nakaabot sa ulap", "Nawala sa bahay" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang naging araw, buwan, at mga bituin?",
                        ChoiceTexts=new(){ "Araw → apoy, Buwan → palayok na may kanin, Bituin → brilyanteng kuwintas", "Araw → palay, Buwan → brilyante, Bituin → apoy", "Araw → buwan, Buwan → bituin, Bituin → araw" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    }
                }
            },
            new Story{
                StoryIndex= 5,Category = "alamat", MedalId = 5,
                Id="5_kungbakitmayulan", Title="Kung bakit may ulan", PriceStars=30, RewardStars=25,
                Thumb = "storiesalamat/kungbakitmayulan/iconkungbakitmayulan.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/kungbakitmayulan.txt","storiesalamat/kungbakitmayulan","kungbakitmayulan"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino ang dalawang pangunahing tauhan sa kwento?",
                        ChoiceTexts=new(){ "Langit at Luntian", "Langit at Araw", "Luntian at Bituin" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Bakit nalulungkot si Luntian sa umpisa?",
                        ChoiceTexts=new(){ "Hindi siya pinapayagang lumikha", "Gutom siya", "Nawawala ang araw" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ni Luntian nang pumunta siya sa malayong bahagi ng kalawakan?",
                        ChoiceTexts=new(){ "Natulog", "Lumikha ng daigdig, karagatan, halaman, hayop, at tao", "Naglaro ng tagu-taguan" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ni Luntian upang mabuhay ang mga tao at hayop?",
                        ChoiceTexts=new(){ "Umihip ng malakas na hangin", "Nagbigay ng pagkain", "Nagbigay ng tubig lamang" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang kulay ng planeta na nakita ni Langit na ginawa ni Luntian?",
                        ChoiceTexts=new(){ "Bughaw at luntian", "Pula at dilaw", "Kahel at berde" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    }
                }
            },
            
            //Epiko
            new Story{
                StoryIndex =21,Category = "epiko", MedalId =6,
                Id="6_Bantugan", Title="Bantugan", PriceStars=20, RewardStars=50,Thumb ="storiesepiko/bantugan/iconbantugan.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/bantugan.txt","storiesepiko/bantugan","bantugan"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino si Bantugan?",
                        ChoiceTexts=new(){ "Mandirigma", "Hari ng ibang lupain", "Kaaway" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Bakit umalis siya?",
                        ChoiceTexts=new(){ "Malungkot at ipinatigil siya ng Haring Madali", "Naglakbay lang", "Nahirapan sa laban" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Sino ang nakakita sa kanya at naghatid ng balita?",
                        ChoiceTexts=new(){ "Prinsesa at loro", "Haring Madali", "Haring Miskoyaw" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ni Haring Madali?",
                        ChoiceTexts=new(){ "Nagsisi at binalik ang kaluluwa ni Bantugan", "Nagdiwang", "Tumakas" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang nangyari sa huli?",
                        ChoiceTexts=new(){ "Namatay siya", "Pinuksa ang kaaway at nagdiwang kasama ang prinsesa", "Iniwan ang kaharian" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    }
                }
            },
            new Story{
                StoryIndex = 22,
                Category = "epiko",
                Id = "7_hinilawod",
                Title = "hinilawod",
                MedalId = 7,
                PriceStars = 20,
                RewardStars = 50,
                Thumb = "storiesepiko/hinilawod/iconhinilawod.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/hinilawod.txt","storiesepiko/hinilawod","hinilawod"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino ang tatlong magkapatid sa pamilya ni Labaw Donggon?",
                        ChoiceTexts=new(){ "Labaw Donggon, Humadapnon, Dumalapdap", "Saragnayan, Malitong Yawa, Baranugan", "Abyang Alunsina, Buyung Paubari, Malitong Yawa" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Sino ang tagapag-alaga ng araw na tinalo si Labaw Donggon?",
                        ChoiceTexts=new(){ "Saragnayan", "Baranugan", "Asu Mangga" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Saan ikinulong si Labaw Donggon?",
                        ChoiceTexts=new(){ "Sa kulungan ng baboy sa ilalim ng bahay", "Sa loob ng lambat", "Sa palasyo" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Sino ang nakatulong kay Labaw Donggon para siya ay mapalaya?",
                        ChoiceTexts=new(){ "Asu Mangga at Buyung Baranugan", "Humadapnon at Dumalapdap", "Abyang Ginbitinan at Anggoy Doronoon" },
                        CorrectIndex=2, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang nangyari sa huli kay Labaw Donggon at sa kaniyang mga asawa?",
                        ChoiceTexts=new(){ "Nag-away sila", "Naging pantay-pantay ang tatlo bilang asawa", "Iniwan ni Malitong Yawa ang kwento" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    }
                }
            },
            new Story{
            StoryIndex = 23,
            Category = "epiko",
            Id = "8_ibalon",
            Title = "Ibalon",
            MedalId = 8,
            PriceStars = 20,
            RewardStars = 50,
            Thumb = "storiesepiko/ibalon/iconibalon.png",
            Slides =  CreateSlidesFromSubtitles(
                "subs/ibalon.txt","storiesepiko/ibalon","ibalon"
                ),
            Quiz = new()
            {
                new QuizQuestion{
                    Prompt="Sino ang nakarating sa lupain ng Ibalon dahil sa baboy-ramo?",
                    ChoiceTexts=new(){ "Handiong", "Baltog", "Bantong" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Sino ang tumulong kay Baltog sa paglipol ng masasamang hayop?",
                    ChoiceTexts=new(){ "Handiong", "Oriol", "Sural" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Sino ang nagturo ng paghabi ng tela sa mga tao?",
                    ChoiceTexts=new(){ "Ginantong", "Hablon", "Dinahong Pandak" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang natutunan ng mga tao sa tulong ni Ginantong?",
                    ChoiceTexts=new(){ "Paghabi ng tela", "Paggawa ng bangka at kasangkapan sa bahay", "Pagsulat" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Paano napatay ni Bantong si Rabut?",
                    ChoiceTexts=new(){ "Habang natutulog", "Sa gitna ng laban", "Sa pamamagitan ng mahiwagang bato" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                }
            }
        },
            new Story{
            StoryIndex = 24,
            Category = "epiko",
            Id = "9_manimimbin",
            Title = "Manimimbin",
            MedalId = 9,
            PriceStars = 20,
            RewardStars = 50,
            Thumb = "storiesepiko/manimimbin/iconmanimimbin.png",
            Slides =  CreateSlidesFromSubtitles(
                "subs/manimimbin.txt","storiesepiko/manimimbin","manimimbin"
                ),
            Quiz = new()
            {
                new QuizQuestion{
                    Prompt="Sino si Manimimbin?",
                    ChoiceTexts=new(){ "Isang binatang naglakbay para maghanap ng asawa", "Isang hari ng Palawan", "Isang mahiwagang ibon" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Sino ang naging kaibigan ni Manimimbin?",
                    ChoiceTexts=new(){ "Ang dalaga na inibig niya", "Kapatid ng dalaga, si Labit", "Kulog" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Bakit nag-away sina Manimimbin at Labit?",
                    ChoiceTexts=new(){ "Dahil may ginto", "Dahil sa di-pagkakaunawaan", "Dahil sa pagkain" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Sino ang humingi ng tulong para magkasundo sila?",
                    ChoiceTexts=new(){ "Binibini ng mga Isda", "Kulog", "Mahiwagang ibon" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang nangyari sa huli kay Manimimbin at Labit?",
                    ChoiceTexts=new(){ "Naghiwalay sila", "Nagkaibigan uli pero hindi nagpakasal", "Nagpakasal at nag-iisang-dibdib" },
                    CorrectIndex=2, TimeLimitSec=20, RewardStars=10
                }
            }
        },
        new Story{
                StoryIndex =25,Category = "epiko", MedalId = 10,
                Id="10_tudbulul", Title="tudbulul", PriceStars=20, RewardStars=50,
                Thumb = "storiesepiko/tudbulul/icontudbulul.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/tudbulul.txt","storiesepiko/tudbulul","tudbulul"
                ),
            Quiz = new()
            {
                new QuizQuestion{
                    Prompt="Sino si Tudbulul?",
                    ChoiceTexts=new(){ "Mandirigma", "Mang-uuma", "Dayuhan" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Bakit siya lumaban?",
                    ChoiceTexts=new(){ "Para sa kababayan", "Para sa ginto", "Para maglakbay" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang ginawa niya sa gitna ng laban?",
                    ChoiceTexts=new(){ "Tumakbo", "Nagbigay lakas sa kasamahan", "Kumain" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang nangyari pagkatapos ng laban?",
                    ChoiceTexts=new(){ "Tinanghal bayani", "Pinarusahan", "Nawala sa bayan" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang alaala niya sa Bukidnon?",
                    ChoiceTexts=new(){ "Inspirasyon sa kalayaan", "Nakalimutan", "Takot sa laban" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                }
            }
            },

            new Story{
                StoryIndex = 31,Category = "pabula", MedalId = 11,
                Id="11_angaralkayloro", Title="angaralkayloro", PriceStars=0, RewardStars=25,Thumb ="storiespabula/angaralkayloro/iconangaralkayloro.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/angaralkayloro.txt","storiespabula/angaralkayloro","angaralkayloro"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Saan naganap ang kuwento ng mga hayop?",
                        ChoiceTexts=new(){ "Sa dagat", "Sa kagubatan", "Sa lungsod" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Bakit naging mayabang si Loro?",
                        ChoiceTexts=new(){ "Dahil sa kanyang kulay at galing magsalita", "Dahil siya ang pinakamataas", "Dahil siya ang pinuno" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ni Loro kay Pagong?",
                        ChoiceTexts=new(){ "Binigyan ng pagkain", "Tinulungan maglakad", "Pinintasan ang mabagal na kilos" },
                        CorrectIndex=2, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Sino ang pinunong nag-ayos ng paligsahan para maturuan si Loro?",
                        ChoiceTexts=new(){ "Si Elepante", "Si Leon", "Si Kuwago" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ni Loro matapos makita ang galing ng ibang hayop?",
                        ChoiceTexts=new(){ "Lumipad palayo", "Humingi ng paumanhin", "Nakipag-away pa lalo" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    }
                }
            },
            new Story{
            StoryIndex = 32,
            Category = "pabula",
            Id = "12_angkabayoatkalabaw",
            Title = "Kabayo at Kalabaw",
            MedalId =  12,
            PriceStars = 20,
            RewardStars = 50,
            Thumb = "storiespabula/angkabayoatkalabaw/iconangkabayoatkalabaw.png",
            Slides =  CreateSlidesFromSubtitles(
                "subs/angkabayoatkalabaw.txt","storiespabula/angkabayoatkalabaw","angkabayoatkalabaw"
                ),
            Quiz = new()
            {
                new QuizQuestion{
                    Prompt="Sino ang mas mabigat ang pasan?",
                    ChoiceTexts=new(){ "Kabayo", "Kalabaw", "Magsasaka" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Bakit humingi ng tulong si Kalabaw kay Kabayo?",
                    ChoiceTexts=new(){ "Para sabay nilang kainin ang pagkain", "Para matulungan sa dalang gulay at prutas", "Para maglaro" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang sinabi ni Kabayo sa una?",
                    ChoiceTexts=new(){ "Sige, tulungan kita", "Ayoko, hindi ako sanay sa mabigat", "Tara, maglakad tayo" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang nangyari sa huli kay Kalabaw?",
                    ChoiceTexts=new(){ "Natumba at nahulog ang pasan", "Tumakbo palayo", "Naglaro kasama ang Kabayo" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Paano natulungan ng magsasaka si Kalabaw sa huli?",
                    ChoiceTexts=new(){ "Tinanggal ang mga dala at inilipat kay Kabayo", "Pinatuloy sa pagdala", "Iniwan sa nayon" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                }
            }
        },
            new Story{
                StoryIndex =33,Category = "pabula", MedalId = 13,
                Id="3_pagongatkuneho", Title="Pagong at Kuneho", PriceStars=20, RewardStars=50,
                Thumb = "storiespabula/pagongatkuneho/iconpagongatkuneho.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/pagongatkuneho.txt","storiespabula/pagongatkuneho","pagongatkuneho"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino ang mas mabagal maglakad?",
                        ChoiceTexts=new(){ "Kuneho", "Pagong", "Matsing" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Bakit pinagtawanan ni Kuneho si Pagong?",
                        ChoiceTexts=new(){ "Dahil mabagal maglakad si Pagong", "Dahil maliit ang bahay ni Pagong", "Dahil may bag sa likod si Pagong" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang hamon ni Pagong kay Kuneho?",
                        ChoiceTexts=new(){ "Magluto sila ng pagkain", "Magkarera patungo sa bundok", "Maglaro ng tagu-taguan" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang ginawa ni Kuneho sa kalagitnaan ng bundok?",
                        ChoiceTexts=new(){ "Natulog dahil sa kumpiyansa", "Tumakbo nang mabilis", "Bumalik sa umpisa" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Sino ang nanalo sa karera?",
                        ChoiceTexts=new(){ "Kuneho", "Pagong", "Matsing" },
                        CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                    }
                }
            },
            new Story{
            StoryIndex = 34,
            Category = "pabula",
            Id = "14_pagong_at_matsing",
            Title = "Pagong at Matsing",
            MedalId = 14,
            PriceStars = 20,
            RewardStars = 50,
            Thumb = "storiespabula/pagongatmatsing/iconpagongatmatsing.png",
            Slides =  CreateSlidesFromSubtitles(
                "subs/pagongatmatsing.txt","storiespabula/pagongatmatsing","pagongatmatsing"
                ),
            Quiz = new()
            {
                new QuizQuestion{
                    Prompt="Sino ang tuso at palabiro?",
                    ChoiceTexts=new(){ "Pagong", "Matsing", "Aling Muning" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang ginawa ni Matsing sa unang bahagi ng kwento?",
                    ChoiceTexts=new(){ "Pinagbigyan si Pagong", "Kinain lahat ng pansit", "Nagbigay ng saging kay Pagong" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Bakit nalanta ang tanim ni Matsing?",
                    ChoiceTexts=new(){ "Hindi niya inalagaan", "Napakaraming ugat", "Tinulungan siya ni Pagong" },
                    CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Paano nainis si Matsing kay Pagong sa dalampasigan?",
                    ChoiceTexts=new(){ "Pinagtakpan si Pagong", "Inisip na matatalo siya sa tubig", "Inihagis ni Matsing sa tubig, pero marunong lumangoy si Pagong" },
                    CorrectIndex=2, TimeLimitSec=20, RewardStars=10
                },
                new QuizQuestion{
                    Prompt="Ano ang natutunan ni Matsing sa huli?",
                    ChoiceTexts=new(){ "Masarap magbiro sa kaibigan", "Hindi dapat lolokohin ang kaibigan", "Magtanim ng saging" },
                    CorrectIndex=1, TimeLimitSec=20, RewardStars=10
                }
            }
        },
        
        new Story{
                StoryIndex =35,Category = "pabula", MedalId = 15,
                Id="3_tularansikawayan", Title="Tularan si Kawayan", PriceStars=20, RewardStars=50,
                Thumb = "storiespabula/tularansikawayan/icontularansikawayan.png",
                Slides =  CreateSlidesFromSubtitles(
                "subs/tularansikawayan.txt","storiespabula/tularansikawayan","tularansikawayan"
                ),
                Quiz = new()
                {
                    new QuizQuestion{
                        Prompt="Sino ang tahimik at hindi pinapansin ng mga puno?",
                        ChoiceTexts=new(){ "Kawayan", "Mangga", "Narra" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang natatanging ginagawa ni Kawayan noong malakas ang hangin?",
                        ChoiceTexts=new(){ "Tumayo at sumabay sa ihip ng hangin", "Bumagsak tulad ng iba", "Nagmamayabang sa mga puno" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang nangyari sa mga puno sa gubat nang humangin?",
                        ChoiceTexts=new(){ "Nabuwal at nasira", "Nakatayo pa rin", "Lumipad" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Sino ang nagbigay ng aral sa mga puno?",
                        ChoiceTexts=new(){ "Haring Sanlibutan", "Hangin", "Kawayan" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    },
                    new QuizQuestion{
                        Prompt="Ano ang aral ng kwento?",
                        ChoiceTexts=new(){ "Maging mapagkumbaba tulad ni Kawayan", "Magmayabang sa sarili", "Tumakbo sa gubat" },
                        CorrectIndex=0, TimeLimitSec=20, RewardStars=10
                    }
                }
            },

        };

        // ------- In-memory runtime (no DB yet) -------
        public static int Stars { get; set; } = 50;
        public static int Hearts { get; set; } = 3;  // daily refill later
        public static string SelectedNarratorId { get; set; } = "tarsier";
        public static string CurrentNarratorImage { get; set; } = "elements/tarsier.png"; // Global variable for narrator image
        
        // Narrator battery system
        public static int NarratorBattery { get; set; } = 3; // 3 = full battery
        public static DateTime LastNarratorUseTime { get; set; } = DateTime.MinValue;
        private static System.Threading.Timer? _narratorBatteryTimer;
        
        public static HashSet<string> UnlockedStories { get; } = new() { "1_juan_tamad" };
        public static HashSet<string> UnlockedNarrators { get; } = new() { "tarsier" };
        public static string category { get; set; } = "";

        // Narrator battery helper methods
        public static string GetNarratorBatteryImage()
        {
            return NarratorBattery switch
            {
                3 => "batteryfull.png",
                2 => "batterythreequarters.png",
                1 => "batteryquarter.png",
                _ => "batteryempty.png"
            };
        }

        public static bool CanUseNarrator()
        {
            CheckAndRefreshNarratorBattery();
            return NarratorBattery > 0;
        }

        public static async Task<bool> UseNarratorAsync()
        {
            if (!CanUseNarrator())
                return false;

            NarratorBattery--;
            LastNarratorUseTime = DateTime.Now;
            
            // Save to database
            await App.Database.UpdateNarratorBatteryAsync(NarratorBattery, LastNarratorUseTime);
            
            // Start 10-minute timer if battery is depleted
            if (NarratorBattery == 0)
            {
                StartNarratorBatteryRefreshTimer();
            }
            
            return true;
        }

        public static void CheckAndRefreshNarratorBattery()
        {
            if (NarratorBattery >= 3)
                return;

            if (LastNarratorUseTime == DateTime.MinValue)
                return;

            var timeSinceLastUse = DateTime.Now - LastNarratorUseTime;
            
            if (timeSinceLastUse.TotalMinutes >= 10)
            {
                NarratorBattery = 3;
                LastNarratorUseTime = DateTime.MinValue; 
                
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await App.Database.UpdateNarratorBatteryAsync(NarratorBattery, LastNarratorUseTime);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to update narrator battery: {ex.Message}");
                    }
                });
                
                _narratorBatteryTimer?.Dispose();
                _narratorBatteryTimer = null;
            }
        }

        private static void StartNarratorBatteryRefreshTimer()
        {
            _narratorBatteryTimer?.Dispose();
            
            _narratorBatteryTimer = new System.Threading.Timer(
                callback: _ => 
                {
                    CheckAndRefreshNarratorBattery();
                    
                    if (NarratorBattery >= 3)
                    {
                        _narratorBatteryTimer?.Dispose();
                        _narratorBatteryTimer = null;
                    }
                },
                state: null,
                dueTime: TimeSpan.FromSeconds(30),
                period: TimeSpan.FromSeconds(30)
            );
        }

        public static void InitializeNarratorBatteryTimer()
        {
            CheckAndRefreshNarratorBattery();
            
            if (NarratorBattery < 3)
            {
                StartNarratorBatteryRefreshTimer();
            }
        }

        public static bool IsStoryUnlocked(string id)
        {
            if (UnlockedStories.Contains(id)) return true;

            var s = Stories.FirstOrDefault(x => x.Id == id);
            if (s == null) return false;

            bool unlocked = s.PriceStars == 0 || s.IsPurchased;

            if (unlocked)
                UnlockedStories.Add(id);

            return unlocked;
        }

        private static readonly HashSet<string> _narratorDbCheckInFlight = new();
        public static string CurrentStoryId { get; set; } = "";
        public static bool MusicIsEnabled { get; set; } = true;
        
        public static bool IsNarratorUnlocked(string id)
        {
            if (id == "tarsier") return true;

            if (UnlockedNarrators.Contains(id)) return true;

            if (id == "eagle")
            {
                var currentStory = Stories.FirstOrDefault(s => s.Id == CurrentStoryId);
                return currentStory != null && currentStory.NarratorEagleUnlocked;
            }
            if (id == "monkey")
            {
                var currentStory = Stories.FirstOrDefault(s => s.Id == CurrentStoryId);
                return currentStory != null && currentStory.NarratorMonkeyUnlocked;
            }

            if (!_narratorDbCheckInFlight.Contains(id))
            {
                _narratorDbCheckInFlight.Add(id);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var dbHas = await App.Database.IsAnyStoryNarratorUnlockedAsync(id);
                        if (dbHas)
                        {
                            UnlockedNarrators.Add(id);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"IsNarratorUnlocked DB check failed for {id}: {ex}");
                    }
                    finally
                    {
                        _narratorDbCheckInFlight.Remove(id);
                    }
                });
            }

            return false;
        }

        public static Narrator CurrentNarrator => Narrators.First(n => n.Id == SelectedNarratorId);
        public static Story GetStory(string id) => Stories.First(s => s.Id == id);

        public static double NarratorVolume { get; set; } = 1.0;
        public static bool TrySpendStars(int amount)
        {
            if (CharacterHelper.CurrentStars < amount) return false;
            return true;
        }
    }
}
