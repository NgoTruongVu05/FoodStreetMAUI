using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FoodStreetMAUI.Models;
using Newtonsoft.Json;

namespace FoodStreetMAUI.Services
{
    public class DataService
    {
        private static string PoiFile =>
            Path.Combine(FileSystem.AppDataDirectory, "pois.json");

        public async Task<List<PointOfInterest>> LoadPoisAsync()
        {
            try
            {
                if (File.Exists(PoiFile))
                {
                    var json = await File.ReadAllTextAsync(PoiFile);
                    return JsonConvert.DeserializeObject<List<PointOfInterest>>(json)
                           ?? GetSamplePois();
                }
            }
            catch { }
            return GetSamplePois();
        }

        public async Task SavePoisAsync(List<PointOfInterest> pois)
        {
            try
            {
                await File.WriteAllTextAsync(PoiFile,
                    JsonConvert.SerializeObject(pois, Formatting.Indented));
            }
            catch { }
        }

        public static List<PointOfInterest> GetSamplePois() => new()
        {
            new()
            {
                Name = "Bánh Mì Hòa Mã", Category = "Bánh mì", Emoji = "🥖",
                Location = new(10.76926, 106.69204),
                TriggerRadius = 25, ApproachRadius = 70, Priority = 9,
                DebounceSeconds = 180,
                Contents = new()
                {
                    ["vi"] = new() { Language="vi", LanguageName="Tiếng Việt",
                        Title="Bánh Mì Hòa Mã – Huyền Thoại 70 Năm",
                        Description="Chào mừng bạn đến Bánh Mì Hòa Mã – tiệm bánh mì lâu đời nhất Sài Gòn, mở cửa từ năm 1958! Ổ bánh mì giòn rụm với nhân pâté, thịt nguội và rau thơm là hương vị không thể quên." },
                    ["en"] = new() { Language="en", LanguageName="English",
                        Title="Hoa Ma Banh Mi – 70-Year Legend",
                        Description="Welcome to Hoa Ma Banh Mi – Saigon's oldest banh mi shop, open since 1958! The crispy baguette filled with pâté, cold cuts and fresh herbs is an unforgettable street food experience." },
                    ["zh"] = new() { Language="zh", LanguageName="中文",
                        Title="和马越南法棍 – 70年传奇",
                        Description="欢迎来到西贡最古老的越南法棍店，自1958年开业！酥脆的法棍夹着肝酱、熟食和新鲜香草，是越南街头美食的经典味道。" },
                    ["ja"] = new() { Language="ja", LanguageName="日本語",
                        Title="ホアマー・バインミー – 70年の歴史",
                        Description="サイゴン最古のバインミー店、1958年創業。パテとコールドカット入りサクサクのバゲットが絶品です。" },
                    ["ko"] = new() { Language="ko", LanguageName="한국어",
                        Title="호아마 반미 – 70년 전통",
                        Description="1958년부터 사이공 최고령 반미 가게! 파테와 냉육이 든 바삭한 바게트는 잊을 수 없는 맛입니다." },
                    ["fr"] = new() { Language="fr", LanguageName="Français",
                        Title="Bánh Mì Hòa Mã – Légende depuis 70 ans",
                        Description="Bienvenue au plus ancien magasin de bánh mì de Saïgon, ouvert depuis 1958! La baguette croustillante au pâté et charcuterie est inoubliable." },
                }
            },
            new()
            {
                Name = "Phở Bắc Hải", Category = "Phở", Emoji = "🍜",
                Location = new(10.76875, 106.69258),
                TriggerRadius = 30, ApproachRadius = 80, Priority = 8,
                DebounceSeconds = 150,
                Contents = new()
                {
                    ["vi"] = new() { Language="vi", LanguageName="Tiếng Việt",
                        Title="Phở Bắc Hải – Vị Bắc Giữa Lòng Nam",
                        Description="Quán phở trứ danh với nước dùng ninh xương bò 12 giờ, thơm lừng hoa hồi và quế. Bánh phở dai, thịt bò tươi, rau giá giòn – một tô phở hoàn hảo." },
                    ["en"] = new() { Language="en", LanguageName="English",
                        Title="Bac Hai Pho – Northern Taste in the South",
                        Description="Famous pho with broth simmered 12 hours with beef bones, star anise and cinnamon. Silky noodles, fresh beef, and crispy bean sprouts." },
                    ["zh"] = new() { Language="zh", LanguageName="中文",
                        Title="北海河粉 – 南方的北方风味",
                        Description="以熬煮12小时的牛骨汤底著称，飘香八角和肉桂。嫩滑的米粉配上新鲜牛肉片和爽脆豆芽。" },
                    ["ja"] = new() { Language="ja", LanguageName="日本語",
                        Title="バックハイ・フォー – 南の街の北部の味",
                        Description="牛骨を12時間煮込んだスープに八角とシナモンが香る有名なフォー店です。" },
                    ["ko"] = new() { Language="ko", LanguageName="한국어",
                        Title="박하이 쌀국수 – 북부의 맛",
                        Description="12시간 동안 우린 쇠뼈 국물에 팔각과 계피 향이 가득한 유명한 쌀국수 가게입니다." },
                    ["fr"] = new() { Language="fr", LanguageName="Français",
                        Title="Phở Bắc Hải – Saveur du Nord au Sud",
                        Description="Fameux phở au bouillon mijoté 12h avec os de bœuf, badiane et cannelle. Nouilles soyeuses, bœuf frais." },
                }
            },
            new()
            {
                Name = "Bún Bò Huế Mụ Rớt", Category = "Bún bò", Emoji = "🌶️",
                Location = new(10.76845, 106.69282),
                TriggerRadius = 25, ApproachRadius = 65, Priority = 7,
                DebounceSeconds = 120,
                Contents = new()
                {
                    ["vi"] = new() { Language="vi", LanguageName="Tiếng Việt",
                        Title="Bún Bò Huế Mụ Rớt – Chuẩn Vị Cố Đô",
                        Description="Nước dùng đỏ au từ sả, mắm ruốc và ớt cay. Sợi bún tròn dai, bắp bò và chả – đúng chuẩn Huế." },
                    ["en"] = new() { Language="en", LanguageName="English",
                        Title="Mu Rot – Authentic Hue Beef Noodle",
                        Description="Red broth with lemongrass, shrimp paste and chili. Thick round noodles, beef and pork rolls – true Hue flavor." },
                    ["zh"] = new() { Language="zh", LanguageName="中文",
                        Title="顺化牛肉米线 – 古都正宗口味",
                        Description="香茅、虾酱和辣椒调味的红色汤底，搭配粗圆米线和牛肉卷。" },
                    ["ja"] = new() { Language="ja", LanguageName="日本語",
                        Title="ムー・ロット – 本格フエビーフヌードル",
                        Description="レモングラス、えびみそ、唐辛子の赤いスープ。太い丸麺と牛肉が絶品。" },
                    ["ko"] = new() { Language="ko", LanguageName="한국어",
                        Title="무 롯 – 정통 후에 소고기 쌀국수",
                        Description="레몬그라스, 새우장, 고추의 붉은 국물에 두꺼운 면발과 소고기." },
                    ["fr"] = new() { Language="fr", LanguageName="Français",
                        Title="Bún Bò Huế – Goût authentique de Hué",
                        Description="Bouillon rouge citronnelle, pâte de crevettes, piment. Nouilles rondes, bœuf et porc." },
                }
            },
            new()
            {
                Name = "Cơm Tấm Thuận Kiều", Category = "Cơm tấm", Emoji = "🍛",
                Location = new(10.76800, 106.69322),
                TriggerRadius = 28, ApproachRadius = 75, Priority = 8,
                DebounceSeconds = 150,
                Contents = new()
                {
                    ["vi"] = new() { Language="vi", LanguageName="Tiếng Việt",
                        Title="Cơm Tấm Thuận Kiều – Linh Hồn Sài Gòn",
                        Description="Cơm tấm thơm, sườn nướng than hoa, bì, chả trứng và nước mắm pha đặc biệt. Linh hồn ẩm thực Sài Gòn!" },
                    ["en"] = new() { Language="en", LanguageName="English",
                        Title="Thuan Kieu Broken Rice – Soul of Saigon",
                        Description="Fragrant broken rice with charcoal-grilled pork ribs, shredded pork skin, egg pork roll and special fish sauce." },
                    ["zh"] = new() { Language="zh", LanguageName="中文",
                        Title="顺桥碎米饭 – 西贡的灵魂",
                        Description="香喷喷碎米饭，炭烤猪排，猪皮丝，特制鱼露。西贡美食的灵魂！" },
                    ["ja"] = new() { Language="ja", LanguageName="日本語",
                        Title="コムタム – サイゴンの魂",
                        Description="香り豊かな砕米ご飯に炭火焼き豚肋骨、特製ヌックマムソースが絶妙。" },
                    ["ko"] = new() { Language="ko", LanguageName="한국어",
                        Title="투안 끼에우 쌀밥 – 사이공의 영혼",
                        Description="향긋한 쌀밥에 숯불구이 돼지갈비와 특제 피쉬소스. 사이공 요리의 영혼!" },
                    ["fr"] = new() { Language="fr", LanguageName="Français",
                        Title="Cơm Tấm – L'âme de Saïgon",
                        Description="Riz brisé parfumé, côtes de porc grillées au charbon, nuoc mam spécial. L'âme de la cuisine saïgonnaise!" },
                }
            },
            new()
            {
                Name = "Chợ Bến Thành", Category = "Chợ", Emoji = "🏛️",
                Location = new(10.77281, 106.69817),
                TriggerRadius = 50, ApproachRadius = 120, Priority = 10,
                DebounceSeconds = 300,
                Contents = new()
                {
                    ["vi"] = new() { Language="vi", LanguageName="Tiếng Việt",
                        Title="Chợ Bến Thành – Biểu Tượng Sài Gòn",
                        Description="Biểu tượng văn hóa hơn 100 năm tuổi của thành phố! Hàng trăm món ăn Nam Bộ, trái cây nhiệt đới và hàng thủ công độc đáo." },
                    ["en"] = new() { Language="en", LanguageName="English",
                        Title="Ben Thanh Market – Icon of Saigon",
                        Description="A cultural icon over 100 years old! Hundreds of Southern Vietnamese dishes, tropical fruits, and local handicrafts." },
                    ["zh"] = new() { Language="zh", LanguageName="中文",
                        Title="滨城市场 – 西贡标志",
                        Description="拥有百年历史的西贡文化标志！数百种南越特色美食、热带水果和手工艺品。" },
                    ["ja"] = new() { Language="ja", LanguageName="日本語",
                        Title="ベンタイン市場 – サイゴンのシンボル",
                        Description="100年以上の文化的シンボル。南ベトナムの名物料理から熱帯フルーツまで勢揃い。" },
                    ["ko"] = new() { Language="ko", LanguageName="한국어",
                        Title="벤탄 시장 – 사이공의 아이콘",
                        Description="100년 역사의 문화 아이콘! 수백 가지 남부 베트남 음식과 열대 과일." },
                    ["fr"] = new() { Language="fr", LanguageName="Français",
                        Title="Marché Bến Thành – Icône de Saïgon",
                        Description="Icône culturelle centenaire! Centaines de spécialités vietnamiennes, fruits tropicaux et artisanat local." },
                }
            },
        };
    }
}
