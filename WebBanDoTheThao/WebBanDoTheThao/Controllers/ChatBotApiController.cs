using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using WebBanDoTheThao.Models;
using System.Text.RegularExpressions;
using System.Drawing;

namespace WebBanDoTheThao.Controllers
{
    [RoutePrefix("api/chatbot")]
    public class ChatBotApiController : ApiController
    {
        QLQAEntities db = new QLQAEntities();

        public class ChatRequest
        {
            public string message { get; set; }
        }
        private readonly string[] _colors = { "đỏ", "xanh", "trắng", "đen", "vàng", "cam", "tím", "hồng", "xám", "nâu", "bạc" };
        private readonly string[] _brands = { "nike", "adidas", "puma", "mizuno", "kamito", "zocker", "mu", "real", "barca", "liverpool", "chelsea", "mc", "arsenal" };
        private readonly string[] _categories = { "áo", "quần", "giày", "găng", "tất", "vớ", "bóng", "banh", "vợt", "balo", "túi" };

        // =================================================================
        // HÀM TÌM KIẾM THÔNG MINH (SMART SEARCH)
        // =================================================================
        private string SmartProductSearch(string ques)
        {
            // 1. Phân tích câu hỏi để tìm các thuộc tính (Entity Extraction)
            string foundColor = _colors.FirstOrDefault(c => ques.Contains(c));
            string foundBrand = _brands.FirstOrDefault(b => ques.Contains(b));
            string foundCategory = _categories.FirstOrDefault(c => ques.Contains(c));

            // 2. Khởi tạo truy vấn
            // Lưu ý: Cần Include THUONGHIEU để tìm theo tên hãng
            var query = db.SANPHAMs.Include("THUONGHIEU").AsQueryable();

            // 3. Áp dụng bộ lọc (Filter)
            bool hasFilter = false;

            if (!string.IsNullOrEmpty(foundCategory))
            {
                query = query.Where(p => p.TENSP.ToLower().Contains(foundCategory));
                hasFilter = true;
            }

            if (!string.IsNullOrEmpty(foundBrand))
            {
                // Tìm trong Tên Thương Hiệu HOẶC Tên Sản Phẩm (VD: Áo MU)
                query = query.Where(p => p.THUONGHIEU.TENTH.ToLower().Contains(foundBrand) || p.TENSP.ToLower().Contains(foundBrand));
                hasFilter = true;
            }

            if (!string.IsNullOrEmpty(foundColor))
            {
                // Tìm trong cột Màu sắc
                query = query.Where(p => p.COLOR.ToLower().Contains(foundColor));
                hasFilter = true;
            }

            // 4. Fallback: Nếu không bắt được từ khóa đặc biệt nào -> Tìm theo từ khóa bất kỳ trong câu
            if (!hasFilter)
            {
                string[] stopWords = { "shop", "có", "không", "muốn", "mua", "bạn", "mình", "cho", "hỏi", "ơi", "tôi", "cần", "tìm" };
                var keywords = ques.Split(' ').Where(w => !stopWords.Contains(w) && w.Length > 1).ToList();

                if (!keywords.Any()) return "Bạn muốn tìm gì nhỉ? Nhắn tên sản phẩm cụ thể giúp mình nha! 😅";

                // Tìm sản phẩm chứa ít nhất 1 từ khóa còn lại
                query = query.Where(p => keywords.Any(k => p.TENSP.ToLower().Contains(k)));
            }

            // 5. Lấy kết quả (Top 5 sản phẩm mới nhất)
            var results = query.OrderByDescending(p => p.ID).Take(5).ToList();

            // 6. Trả về HTML
            if (results.Any())
            {
                string intro = "Dạ, mình tìm thấy các mẫu ";
                if (!string.IsNullOrEmpty(foundCategory)) intro += foundCategory + " ";
                if (!string.IsNullOrEmpty(foundBrand)) intro += "hiệu " + System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(foundBrand) + " ";
                if (!string.IsNullOrEmpty(foundColor)) intro += "màu " + foundColor + " ";
                intro += "này cho bạn nè:<br/>";

                return intro + GetProductHtml(results);
            }
            else
            {
                string msg = "Tiếc quá, shop hiện chưa có ";
                if (!string.IsNullOrEmpty(foundCategory)) msg += foundCategory + " ";
                if (!string.IsNullOrEmpty(foundBrand)) msg += "của " + foundBrand + " ";
                if (!string.IsNullOrEmpty(foundColor)) msg += "màu " + foundColor;

                msg += " rồi ạ. Bạn thử tìm mẫu khác xem sao nhé? 😢";
                return msg;
            }
        }

        // =================================================================
        // HÀM FORMAT SẢN PHẨM THÀNH HTML (GỬI CHO CHATBOT HIỂN THỊ)
        // =================================================================
        private string GetProductHtml(List<SANPHAM> products)
        {
            if (products == null || !products.Any()) return "";

            // CSS inline để tạo thanh cuộn ngang (giống Shopee/Messenger)
            string html = "<div style='display:flex; overflow-x:auto; gap:10px; padding-bottom:5px; scrollbar-width: thin;'>";

            foreach (var sp in products)
            {
                string img = "/Assets/Clothes_Images/" + (sp.AVATAR ?? "default.jpg");
                string link = "/Home/ShowFullProduct/" + sp.ID;

                html += $@"
        <a href='{link}' target='_blank' style='text-decoration:none; color:inherit; flex: 0 0 auto;'>
            <div style='width:130px; border:1px solid #eee; border-radius:10px; padding:8px; background:#fff; box-shadow: 0 2px 4px rgba(0,0,0,0.05);'>
                <img src='{img}' onerror=""this.src='/Assets/images/no-image.png'"" style='width:100%; height:110px; object-fit:cover; border-radius:8px; margin-bottom:5px;'>
                <div style='font-size:12px; font-weight:600; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; margin-bottom:3px;'>{sp.TENSP}</div>
                <div style='color:#d9534f; font-weight:bold; font-size:13px;'>{sp.GIA:N0}đ</div>
                <div style='font-size:10px; color:#fff; background:#007bff; text-align:center; border-radius:4px; padding:2px; margin-top:5px;'>Xem ngay</div>
            </div>
        </a>";
            }

            html += "</div>";
            return html;
        }

        // ==========================================
        // API CHÍNH
        // ==========================================
        [HttpPost]
        [Route("ask")]
        public IHttpActionResult Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.message))
                return Ok(new { reply = "Bạn chưa nhập gì mà! 😅" });

            string ques = request.message.ToLower().Trim();
            string reply = "";
            var session = HttpContext.Current.Session;
            string currentContext = session["BotContext"] as string;

            // --- 1. ƯU TIÊN CAO NHẤT: BẮT TỪ KHÓA TÌM KIẾM (ENTITY EXTRACTION) ---
            // Nếu khách nhắc đến Màu, Hãng, hoặc Loại -> Tìm hàng ngay lập tức!
            // (Khắc phục lỗi: "Muốn mua áo đỏ" mà Bot lại trả lời chung chung)

            bool hasColor = _colors.Any(c => ques.Contains(c));
            bool hasBrand = _brands.Any(b => ques.Contains(b));
            bool hasCategory = _categories.Any(c => ques.Contains(c));

            // Nếu phát hiện ít nhất 1 đặc điểm sản phẩm VÀ câu không phải là hỏi size/ship/đổi trả
            if ((hasColor || hasBrand || hasCategory) && !ques.Contains("size") && !ques.Contains("ship") && !ques.Contains("đổi"))
            {
                reply = SmartProductSearch(ques);
                return Ok(new { reply });
            }

            // --- 2. XỬ LÝ NGỮ CẢNH CŨ (Khi khách đang nhập size...) ---
            if (currentContext == "cho_nhap_size")
            {
                string sizeResult = CalculateSize(ques);
                if (sizeResult != null)
                {
                    session["BotContext"] = null;
                    return Ok(new { reply = sizeResult });
                }
                // Nếu không phải số đo thì để code chạy tiếp xuống dưới xử lý
            }

            // --- 3. PHÂN LOẠI Ý ĐỊNH (NAIVE BAYES) ---
            // Chỉ chạy khi không tìm thấy từ khóa sản phẩm cụ thể
            string intent = DetectIntent(ques);

            switch (intent)
            {
                case "greeting": reply = "Chào bạn! SportShop nghe đây ạ. Bạn cần tìm giày, áo hay tư vấn size?"; break;

                case "store_info": reply = "Shop ở 123 Đường Thể Thao, TP.HCM ạ. Mở cửa 8h-22h hàng ngày."; break;

                case "promotion": reply = "Hiện tại đơn trên 500k là được Freeship đó bạn ơi! 😍"; break;

                case "contact": reply = "Hotline/Zalo của shop: 0909.123.456 nha."; break;

                case "complaint": reply = "Dạ bạn yên tâm, hàng lỗi shop bao đổi trả trong 7 ngày ạ. Bạn inbox Zalo để shop xử lý nhanh nha."; break;

                case "price_inquiry": reply = "Giá bên mình cực tốt, áo từ 150k, giày từ 300k. Bạn bấm vào sản phẩm để xem chi tiết nhé!"; break;

                case "size_advice":
                    reply = "Bạn cho mình xin <b>Chiều cao</b> và <b>Cân nặng</b> (VD: 1m70 60kg) để mình tính size cho chuẩn nhé!";
                    session["BotContext"] = "cho_nhap_size";
                    break;

                case "shipping": reply = "Ship nội thành 20k, tỉnh 30k. Tầm 2-3 ngày là nhận được ạ."; break;

                // Trường hợp hỏi chung chung "muốn mua đồ", "có giày không" (không cụ thể loại nào)
                case "product_general":
                    if (ques.Contains("giày")) reply = "Bạn thích giày <b>Nike</b>, <b>Adidas</b> hay <b>Mizuno</b>?";
                    else if (ques.Contains("áo")) reply = "Bạn tìm áo <b>CLB</b> hay <b>Đội tuyển</b>?";
                    else reply = "Dạ shop có đủ đồ thể thao. Bạn tìm <b>Giày</b>, <b>Quần áo</b> hay <b>Phụ kiện</b>?";
                    break;

                default: // Không hiểu gì hết -> Vẫn cố tìm kiếm lần cuối (Vớt vát)
                    reply = SmartProductSearch(ques);
                    break;
            }

            return Ok(new { reply });
        }

        // ==========================================
        // CÁC HÀM LOGIC BỔ TRỢ (HELPER FUNCTIONS)
        // ==========================================


        // Hàm đoán ý định (Kết hợp từ khóa + Naive Bayes cũ của bạn)
        private string DetectIntent(string text)
        {
            // Ưu tiên bắt từ khóa cứng trước (Rule-based)
            if (text.Contains("địa chỉ") || text.Contains("ở đâu")) return "store_info";
            if (text.Contains("khuyến mãi") || text.Contains("giảm giá") || text.Contains("voucher")) return "promotion";
            if (text.Contains("điện thoại") || text.Contains("sđt") || text.Contains("hotline") || text.Contains("zalo")) return "contact";
            if (text.Contains("lỗi") || text.Contains("rách") || text.Contains("hỏng") || text.Contains("đổi trả")) return "complaint";
            if (text.Contains("size") || text.Contains("vừa") || text.Contains("rộng") || text.Contains("chật") || text.Contains("cân nặng")) return "size_advice";
            if (text.Contains("giá") || text.Contains("nhiêu")) return "price_inquiry";
            if (text.Contains("ship") || text.Contains("giao hàng") || text.Contains("vận chuyển")) return "shipping";

            // Nếu không bắt được rule thì dùng Naive Bayes (Bạn nhớ cập nhật file training_data.txt)
            string nbIntent = NaiveBayes(text);
            if (nbIntent != "unknown") return nbIntent;

            // Mặc định là hỏi về sản phẩm
            return "product_general";
        }


        // --- CHÚ Ý: DƯỚI ĐÂY LÀ PHẦN CODE CŨ (NaiveBayes, GetTraining...) BẠN GIỮ NGUYÊN NHÉ ---
        // (Copy lại từ file cũ của bạn vào)
        private string CalculateSize(string input)
        {
            input = input.ToLower();
            input = Regex.Replace(input, @"(\d)\s*m\s*(\d*)", "$1.$2");
            input = input.Replace(',', '.');
            double height = 0;
            double weight = 0;
            var numbers = Regex.Matches(input, @"[0-9]+(\.[0-9]+)?");
            foreach (Match match in numbers)
            {
                if (double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    if ((val < 2.5 && val > 1.0) || val > 100) { if (val < 2.5) height = val * 100; else height = val; }
                    else if (val >= 30 && val <= 100) weight = val;
                }
            }
            if (height == 0 || weight == 0) return null;
            string size = "";
            if (height < 160 || weight < 50) size = "S";
            else if (height < 167 || weight < 60) size = "M";
            else if (height < 175 || weight < 70) size = "L";
            else if (height < 180 || weight < 80) size = "XL";
            else size = "XXL";
            return $"Với chiều cao <b>{height:0}cm</b> và cân nặng <b>{weight:0}kg</b>, bạn mặc size <b>{size}</b> là đẹp nhất ạ! 🥰";
        }

        private string NaiveBayes(string cauHoi)
        {
            var session = HttpContext.Current.Session;
            List<(string Text, string Intent)> DataTrain;
            if (session["DataTrain"] == null)
            {
                string path = HttpContext.Current.Server.MapPath("~/App_Data/training_data.txt");
                DataTrain = GetTraining(path);
                session["DataTrain"] = DataTrain;
            }
            else { DataTrain = (List<(string Text, string Intent)>)session["DataTrain"]; }

            if (DataTrain == null || DataTrain.Count == 0) return "unknown";

            var intentCounts = new Dictionary<string, int>();
            var wordCounts = new Dictionary<string, Dictionary<string, int>>();
            var totalWordsInIntent = new Dictionary<string, int>();

            foreach (var (text, intent) in DataTrain)
            {
                if (!intentCounts.ContainsKey(intent))
                {
                    intentCounts[intent] = 0; wordCounts[intent] = new Dictionary<string, int>(); totalWordsInIntent[intent] = 0;
                }
                intentCounts[intent]++;
                var words = text.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (!wordCounts[intent].ContainsKey(word)) wordCounts[intent][word] = 0;
                    wordCounts[intent][word]++; totalWordsInIntent[intent]++;
                }
            }

            double totalDocuments = DataTrain.Count;
            int vocabularySize = wordCounts.Values.SelectMany(d => d.Keys).Distinct().Count();
            var newWords = cauHoi.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string bestIntent = "unknown"; double highestScore = double.MinValue;

            foreach (var intent in intentCounts.Keys)
            {
                double priorProb = intentCounts[intent] / totalDocuments;
                double logScore = Math.Log(priorProb);
                foreach (var word in newWords)
                {
                    int count = 0;
                    if (wordCounts[intent].ContainsKey(word)) count = wordCounts[intent][word];
                    double wordCountSmoothed = count + 1;
                    double totalWordsSmoothed = totalWordsInIntent[intent] + vocabularySize;
                    logScore += Math.Log(wordCountSmoothed / totalWordsSmoothed);
                }
                if (logScore > highestScore) { highestScore = logScore; bestIntent = intent; }
            }
            return bestIntent;
        }

        private List<(string Text, string Intent)> GetTraining(string filePath)
        {
            var result = new List<(string Text, string Intent)>();
            if (!System.IO.File.Exists(filePath)) return result;
            var lines = System.IO.File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                if (parts.Length >= 2) result.Add((parts[0].Trim(), parts[1].Trim()));
            }
            return result;
        }
    }
}