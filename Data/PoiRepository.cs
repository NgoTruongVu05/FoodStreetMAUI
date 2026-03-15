using FoodStreetGuide.Models;

namespace FoodStreetGuide.Data;

/// <summary>
/// Repository POI – có thể inject dữ liệu JSON, API, hoặc hardcode (demo).
/// </summary>
public interface IPoiRepository
{
    Task<IReadOnlyList<PointOfInterest>> GetAllAsync();
}

/// <summary>
/// Dữ liệu mẫu: Phố ẩm thực Nguyễn Công Trứ, TP.HCM.
/// Trong production: thay bằng JSON file hoặc REST API.
/// </summary>
public sealed class SamplePoiRepository : IPoiRepository
{
    public Task<IReadOnlyList<PointOfInterest>> GetAllAsync()
    {
        var pois = new List<PointOfInterest>
        {
            // ── POI 1: Bún bò Huế Cô Ba ─────────────────────────────────────
            new(
                zone: new GeofenceZone(
                    center: new GeoLocation(10.77690, 106.70088),
                    triggerRadius: 30)
                {
                    DebounceSeconds = 120
                },
                content: new LocalizedContentMap()
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Vietnamese,
                        Title       = "Bún bò Huế Cô Ba",
                        Description = "Quán bún bò nổi tiếng hơn 30 năm tại phố Nguyễn Công Trứ. " +
                                      "Nước dùng được ninh từ xương heo và sả, tạo nên hương vị đậm đà " +
                                      "đặc trưng miền Trung không thể nhầm lẫn. Mở cửa từ 6 đến 10 giờ sáng.",
                        AudioFile   = "audio/bun_bo_hue_vi.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.English,
                        Title       = "Co Ba Hue Beef Noodle",
                        Description = "A legendary noodle stall operating for over 30 years. " +
                                      "The broth is slow-simmered with pork bones and lemongrass, " +
                                      "delivering the unmistakable bold flavor of Central Vietnam. Open 6–10 AM.",
                        AudioFile   = "audio/bun_bo_hue_en.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Chinese,
                        Title       = "阿三顺化牛肉米线",
                        Description = "在这条街上已有三十余年历史的著名米线摊。汤底由猪骨与柠檬草" +
                                      "慢火熬制，具有浓郁的中越风味，令人难忘。营业时间早六时至十时。",
                        AudioFile   = "audio/bun_bo_hue_zh.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Japanese,
                        Title       = "コーバー フエ牛肉麺",
                        Description = "30年以上の歴史を誇る伝説の麺スタンドです。豚骨とレモングラスで" +
                                      "丁寧に煮込んだスープは、ベトナム中部独特の濃厚な風味が特徴です。" +
                                      "営業時間は朝6時から10時まで。",
                        AudioFile   = "audio/bun_bo_hue_ja.mp3"
                    })
            )
            {
                Emoji    = "🍜",
                Priority = PoiPriority.Hero
            },

            // ── POI 2: Gỏi cuốn Mẹ Tư ──────────────────────────────────────
            new(
                zone: new GeofenceZone(
                    center: new GeoLocation(10.77721, 106.70142),
                    triggerRadius: 25),
                content: new LocalizedContentMap()
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Vietnamese,
                        Title       = "Gỏi cuốn Mẹ Tư",
                        Description = "Gỏi cuốn tươi với tôm tươi đánh bắt buổi sáng, thịt luộc mềm," +
                                      " rau sống và bún. Chấm với nước mắm chua ngọt theo công thức gia truyền " +
                                      "ba thế hệ. Một trong những địa chỉ không thể bỏ qua.",
                        AudioFile   = "audio/goi_cuon_vi.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.English,
                        Title       = "Me Tu Fresh Spring Rolls",
                        Description = "Fresh rice paper rolls packed with morning-caught shrimp, tender " +
                                      "boiled pork, herbs, and vermicelli. Served with a legendary sweet " +
                                      "fish sauce dip — a 3-generation family secret. A must-visit.",
                        AudioFile   = "audio/goi_cuon_en.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Chinese,
                        Title       = "四嫂越南春卷",
                        Description = "新鲜米纸卷配当日清晨打捞的虾仁、白切肉、香草和细米线。" +
                                      "搭配三代传承秘方甜酸鱼露，是这条街上不可错过的美食之一。"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Japanese,
                        Title       = "メートゥー 生春巻き",
                        Description = "朝に水揚げされた新鮮なエビ、柔らかい蒸し豚、フレッシュハーブと" +
                                      "ビーフンを包んだ生春巻き。3代続く秘伝の甘酸っぱいナンプラーダレで" +
                                      "どうぞ。ぜひ立ち寄ってみてください。"
                    })
            )
            {
                Emoji    = "🥗",
                Priority = PoiPriority.High
            },

            // ── POI 3: Bánh mì Hùng ─────────────────────────────────────────
            new(
                zone: new GeofenceZone(
                    center: new GeoLocation(10.77655, 106.70215),
                    triggerRadius: 20),
                content: new LocalizedContentMap()
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Vietnamese,
                        Title       = "Bánh mì Hùng",
                        Description = "Ổ bánh mì giòn rụm với nhân thịt heo quay, pate thơm, " +
                                      "rau thơm tươi và tương ớt đặc biệt. Nổi danh trên mạng xã hội, " +
                                      "hàng dài mỗi buổi sáng — đến sớm để không phải đợi lâu!",
                        AudioFile   = "audio/banh_mi_vi.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.English,
                        Title       = "Hung Banh Mi",
                        Description = "A golden-crusted baguette stuffed with roasted pork, aromatic pate, " +
                                      "fresh herbs, and a signature chili sauce. Went viral on social media — " +
                                      "expect queues in the morning, so arrive early!",
                        AudioFile   = "audio/banh_mi_en.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Chinese,
                        Title       = "雄越南三明治",
                        Description = "外皮酥脆的法式长棍面包，内馅为烤猪肉、浓香肉酱、" +
                                      "新鲜香草和特制辣酱。在社交媒体上走红，每天早上大排长龙，" +
                                      "建议早点来！"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Japanese,
                        Title       = "フン・バインミー",
                        Description = "パリパリのバゲットに焼き豚、香り高いパテ、新鮮なハーブ、" +
                                      "特製チリソースを詰めたベトナムサンドイッチ。SNSで話題になり、" +
                                      "毎朝行列ができます。早めにどうぞ！"
                    })
            )
            {
                Emoji    = "🥖",
                Priority = PoiPriority.High
            },

            // ── POI 4: Chè Ngọc Hương ───────────────────────────────────────
            new(
                zone: new GeofenceZone(
                    center: new GeoLocation(10.77598, 106.70175),
                    triggerRadius: 30),
                content: new LocalizedContentMap()
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Vietnamese,
                        Title       = "Chè Ngọc Hương",
                        Description = "Chè ba màu đặc sắc và trà sữa thơm ngon giải nhiệt hoàn hảo " +
                                      "sau khi dạo phố. Nguyên liệu đậu tươi và thạch tự làm mỗi ngày. " +
                                      "Không gian mát lạnh với ghế ngồi thoải mái.",
                        AudioFile   = "audio/che_vi.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.English,
                        Title       = "Ngoc Huong Desserts",
                        Description = "Vibrant 3-color jelly desserts and creamy milk tea — the perfect " +
                                      "cool-down after a street food walk. Fresh beans and handmade jelly " +
                                      "prepared daily. Comfortable air-conditioned seating.",
                        AudioFile   = "audio/che_en.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Chinese,
                        Title       = "玉香甜品",
                        Description = "色彩鲜艳的三色冻甜品和奶茶，是逛完美食街后的完美消暑选择。" +
                                      "每日现制新鲜豆类与手工冻，提供舒适的空调座位。"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Japanese,
                        Title       = "ゴックフォン スイーツ",
                        Description = "カラフルな3色ゼリーデザートとクリーミーなミルクティーで、" +
                                      "屋台めぐりの後にひと息つけます。毎日手作りの豆とゼリーを使用。" +
                                      "快適なエアコン完備の座席あり。"
                    })
            )
            {
                Emoji    = "🧋",
                Priority = PoiPriority.Normal
            },

            // ── POI 5: Lẩu Thái Sơn Nam ─────────────────────────────────────
            new(
                zone: new GeofenceZone(
                    center: new GeoLocation(10.77540, 106.70253),
                    triggerRadius: 35),
                content: new LocalizedContentMap()
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Vietnamese,
                        Title       = "Lẩu Thái Sơn Nam",
                        Description = "Lẩu Thái chua cay đặc trưng với hải sản tươi nhập ngày. " +
                                      "Không gian rộng rãi ngoài trời, phù hợp cho nhóm bạn hay gia đình. " +
                                      "Mở cửa từ 11 giờ trưa đến 22 giờ đêm.",
                        AudioFile   = "audio/lau_thai_vi.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.English,
                        Title       = "Son Nam Thai Hot Pot",
                        Description = "Authentic sour-and-spicy Thai hot pot with daily-fresh seafood. " +
                                      "Spacious outdoor seating — great for groups and families. " +
                                      "Open from 11 AM to 10 PM.",
                        AudioFile   = "audio/lau_thai_en.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Chinese,
                        Title       = "山南泰式火锅",
                        Description = "正宗酸辣泰式火锅，配以每日新鲜海鲜。宽敞的户外就座区，" +
                                      "适合朋友聚会或家庭用餐。营业时间上午11时至晚上10时。"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Japanese,
                        Title       = "ソンナム タイ鍋",
                        Description = "毎日仕入れる新鮮な海鮮を使った本格タイ風酸っぱ辛い鍋。" +
                                      "広々とした屋外席はグループや家族連れに最適。" +
                                      "営業時間は午前11時から午後10時まで。"
                    })
            )
            {
                Emoji    = "🍲",
                Priority = PoiPriority.Normal
            },

            // ── POI 6: Bánh tráng nướng Dì Năm ─────────────────────────────
            new(
                zone: new GeofenceZone(
                    center: new GeoLocation(10.77488, 106.70318),
                    triggerRadius: 20)
                {
                    DebounceSeconds = 90
                },
                content: new LocalizedContentMap()
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Vietnamese,
                        Title       = "Bánh tráng nướng Dì Năm",
                        Description = "Bánh tráng nướng trên than hoa với trứng cút, phô mai, " +
                                      "tôm khô và hành lá thơm phức. Chỉ bán vào buổi tối từ 17 giờ đến 23 giờ. " +
                                      "Là địa chỉ check-in nổi tiếng của giới trẻ Sài Gòn.",
                        AudioFile   = "audio/banh_trang_nuong_vi.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.English,
                        Title       = "Di Nam Grilled Rice Paper",
                        Description = "Rice paper grilled over charcoal, topped with quail eggs, melted " +
                                      "cheese, dried shrimp, and green onions. Available evenings only, " +
                                      "5–11 PM. A popular Instagram spot for Saigon youth.",
                        AudioFile   = "audio/banh_trang_nuong_en.mp3"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Chinese,
                        Title       = "五姨烤米纸",
                        Description = "木炭烤制的米纸上铺满鹌鹑蛋、融化奶酪、虾米和葱花，" +
                                      "香气四溢。仅限晚间17时至23时营业。是西贡年轻人打卡的热门地点。"
                    })
                    .Add(new LocalizedContent
                    {
                        Language    = AppLanguage.Japanese,
                        Title       = "ディナム 焼きライスペーパー",
                        Description = "炭火で焼いたライスペーパーにうずら卵、溶けたチーズ、" +
                                      "干しエビ、青ネギをトッピング。夕方のみ17時から23時の営業です。" +
                                      "サイゴンの若者に人気のインスタスポット。"
                    })
            )
            {
                Emoji    = "🍡",
                Priority = PoiPriority.Normal
            }
        };

        return Task.FromResult<IReadOnlyList<PointOfInterest>>(pois.AsReadOnly());
    }
}
