using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Flipped_Classroom.Models;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Data
{
    public static class DbInitializer
    {
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public static void Initialize(Swp391NihongoContext context)
        {
            try
            {
                // Thử áp dụng migrations nếu chưa tạo bảng
                context.Database.Migrate();
            }
            catch
            {
                // Bỏ qua nếu DB đã sẵn sàng
            }

            var defaultPasswordHash = HashPassword("123456");

            // ==========================================
            // 1. SEED / UPSERT USERS (Admin, Manager, Teacher, Student)
            // ==========================================
            var usersToEnsure = new List<User>
            {
                new User
                {
                    Username = "admin",
                    PasswordHash = defaultPasswordHash,
                    Email = "admin@nihongo.edu.vn",
                    FirstName = "Hệ thống",
                    LastName = "Admin",
                    Gender = "Nam",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    PhoneNumber = "0901000001",
                    Address = "Hà Nội"
                },
                new User
                {
                    Username = "manager",
                    PasswordHash = defaultPasswordHash,
                    Email = "manager@nihongo.edu.vn",
                    FirstName = "Trần",
                    LastName = "Quản Lý",
                    Gender = "Nữ",
                    Role = "Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    PhoneNumber = "0901000002",
                    Address = "Hà Nội"
                },
                new User
                {
                    Username = "teacher1",
                    PasswordHash = defaultPasswordHash,
                    Email = "tanaka.sensei@nihongo.edu.vn",
                    FirstName = "Tanaka",
                    LastName = "Kenji",
                    Gender = "Nam",
                    Role = "Teacher",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    PhoneNumber = "0901000003",
                    Address = "Tokyo / Hà Nội"
                },
                new User
                {
                    Username = "teacher2",
                    PasswordHash = defaultPasswordHash,
                    Email = "yamada.sensei@nihongo.edu.vn",
                    FirstName = "Yamada",
                    LastName = "Sakura",
                    Gender = "Nữ",
                    Role = "Teacher",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    PhoneNumber = "0901000004",
                    Address = "Osaka / Hà Nội"
                },
                new User
                {
                    Username = "student1",
                    PasswordHash = defaultPasswordHash,
                    Email = "nguyenvana@gmail.com",
                    FirstName = "Nguyễn",
                    LastName = "Văn A",
                    Gender = "Nam",
                    Role = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    PhoneNumber = "0902000001",
                    Address = "Cầu Giấy, Hà Nội"
                },
                new User
                {
                    Username = "student2",
                    PasswordHash = defaultPasswordHash,
                    Email = "tranthib@gmail.com",
                    FirstName = "Trần",
                    LastName = "Thị B",
                    Gender = "Nữ",
                    Role = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    PhoneNumber = "0902000002",
                    Address = "Đống Đa, Hà Nội"
                },
                new User
                {
                    Username = "student3",
                    PasswordHash = defaultPasswordHash,
                    Email = "levanc@gmail.com",
                    FirstName = "Lê",
                    LastName = "Văn C",
                    Gender = "Nam",
                    Role = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    PhoneNumber = "0902000003",
                    Address = "Thanh Xuân, Hà Nội"
                },
                new User
                {
                    Username = "student4",
                    PasswordHash = defaultPasswordHash,
                    Email = "phamthid@gmail.com",
                    FirstName = "Phạm",
                    LastName = "Thị D",
                    Gender = "Nữ",
                    Role = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    PhoneNumber = "0902000004",
                    Address = "Ba Đình, Hà Nội"
                }
            };

            foreach (var u in usersToEnsure)
            {
                var existing = context.Users.FirstOrDefault(x => x.Username == u.Username);
                if (existing == null)
                {
                    // Kiểm tra email nếu trùng thì đổi tạm
                    if (context.Users.Any(x => x.Email == u.Email))
                    {
                        u.Email = $"{u.Username}_{Guid.NewGuid().ToString("N").Substring(0, 4)}@nihongo.edu.vn";
                    }
                    context.Users.Add(u);
                }
                else
                {
                    // Đảm bảo mật khẩu là 123456 và active
                    existing.PasswordHash = defaultPasswordHash;
                    existing.IsActive = true;
                    existing.Role = u.Role;
                }
            }
            context.SaveChanges();

            var managerUser = context.Users.First(u => u.Role == "Manager" || u.Username == "manager");
            var teacher1 = context.Users.First(u => u.Username == "teacher1");
            var teacher2 = context.Users.First(u => u.Username == "teacher2");
            var student1 = context.Users.First(u => u.Username == "student1");
            var student2 = context.Users.First(u => u.Username == "student2");
            var student3 = context.Users.First(u => u.Username == "student3");
            var student4 = context.Users.First(u => u.Username == "student4");

            // ==========================================
            // 2. SEED CURRICULUM (Khung chương trình)
            // ==========================================
            var curriculumN5 = context.Curriculums.FirstOrDefault(c => c.CurriculumName.Contains("N5"));
            if (curriculumN5 == null)
            {
                curriculumN5 = new Curriculum
                {
                    CurriculumName = "Tiếng Nhật Sơ Cấp N5 - Minna no Nihongo I",
                    Description = "Khung chương trình chuẩn bị kiến thức nền tảng tiếng Nhật trình độ N5 gồm bảng chữ cái, ngữ pháp cơ bản, từ vựng và luyện phát âm.",
                    ManagerId = managerUser.Id,
                    CreatedAt = DateTime.Now.AddMonths(-3)
                };
                context.Curriculums.Add(curriculumN5);
                context.SaveChanges();
            }

            var curriculumN4 = context.Curriculums.FirstOrDefault(c => c.CurriculumName.Contains("N4"));
            if (curriculumN4 == null)
            {
                curriculumN4 = new Curriculum
                {
                    CurriculumName = "Tiếng Nhật Sơ Trung Cấp N4 - Minna no Nihongo II",
                    Description = "Khung chương trình nâng cao kiến thức ngữ pháp mẫu câu trung cấp, kính ngữ, thể bị động, sai khiến.",
                    ManagerId = managerUser.Id,
                    CreatedAt = DateTime.Now.AddMonths(-2)
                };
                context.Curriculums.Add(curriculumN4);
                context.SaveChanges();
            }

            // ==========================================
            // 3. SEED NODES (Chương / Bài học)
            // ==========================================
            if (!context.Nodes.Any(n => n.CurriculumId == curriculumN5.Id))
            {
                var node1 = new Node
                {
                    CurriculumId = curriculumN5.Id,
                    Title = "Bài 1: Chào hỏi & Giới thiệu bản thân (自己紹介)",
                    Description = "Học cách tự giới thiệu tên, tuổi, quốc tịch, nghề nghiệp. Cấu trúc N1 wa N2 desu.",
                    NodeOrder = 1,
                    Status = "Published",
                    IsActive = true,
                    IsReviewNode = false,
                    EstimatedMinutes = 60
                };

                var node2 = new Node
                {
                    CurriculumId = curriculumN5.Id,
                    Title = "Bài 2: Đồ vật & Sở hữu (これ・それ・あれ)",
                    Description = "Học các đại từ chỉ định đồ vật Kore, Sore, Are, Kono N, Sono N, Ano N và cấu trúc sở hữu No.",
                    NodeOrder = 2,
                    Status = "Published",
                    IsActive = true,
                    IsReviewNode = false,
                    EstimatedMinutes = 60
                };

                var node3 = new Node
                {
                    CurriculumId = curriculumN5.Id,
                    Title = "Bài 3: Địa điểm & Phương hướng (ここ・そこ・あそこ)",
                    Description = "Học cách hỏi và chỉ vị trí, phòng ốc, tòa nhà, đất nước, giá tiền.",
                    NodeOrder = 3,
                    Status = "Published",
                    IsActive = true,
                    IsReviewNode = false,
                    EstimatedMinutes = 75
                };

                var node4 = new Node
                {
                    CurriculumId = curriculumN5.Id,
                    Title = "Bài 4: Thời gian & Động từ sinh hoạt (今～時・動詞)",
                    Description = "Học cách nói giờ giấc, các ngày trong tuần, động từ thức dậy, đi ngủ, làm việc (V-masu).",
                    NodeOrder = 4,
                    Status = "Published",
                    IsActive = true,
                    IsReviewNode = false,
                    EstimatedMinutes = 90
                };

                var node5 = new Node
                {
                    CurriculumId = curriculumN5.Id,
                    Title = "Bài 5: Di chuyển & Phương tiện (行きます・来ます・帰ります)",
                    Description = "Học cách nói đi đâu, bằng phương tiện gì, đi với ai.",
                    NodeOrder = 5,
                    Status = "Published",
                    IsActive = true,
                    IsReviewNode = false,
                    EstimatedMinutes = 90
                };

                context.Nodes.AddRange(node1, node2, node3, node4, node5);
                context.SaveChanges();

                // 4. Vocabularies
                context.Vocabularies.AddRange(
                    new Vocabulary { NodeId = node1.Id, Word = "私", Hiragana = "わたし", Romaji = "watashi", Meaning = "Tôi" },
                    new Vocabulary { NodeId = node1.Id, Word = "学生", Hiragana = "がくせい", Romaji = "gakusei", Meaning = "Học sinh, sinh viên" },
                    new Vocabulary { NodeId = node1.Id, Word = "先生", Hiragana = "せんせい", Romaji = "sensei", Meaning = "Thầy / Cô giáo" },
                    new Vocabulary { NodeId = node1.Id, Word = "会社員", Hiragana = "かいしゃいん", Romaji = "kaishain", Meaning = "Nhân viên công ty" },
                    new Vocabulary { NodeId = node1.Id, Word = "日本人", Hiragana = "にほんじん", Romaji = "nihonjin", Meaning = "Người Nhật Bản" },
                    new Vocabulary { NodeId = node1.Id, Word = "本", Hiragana = "ほん", Romaji = "hon", Meaning = "Quyển sách" },
                    new Vocabulary { NodeId = node2.Id, Word = "辞書", Hiragana = "じしょ", Romaji = "jisho", Meaning = "Từ điển" },
                    new Vocabulary { NodeId = node2.Id, Word = "鍵", Hiragana = "かぎ", Romaji = "kagi", Meaning = "Chìa khóa" },
                    new Vocabulary { NodeId = node3.Id, Word = "教室", Hiragana = "きょうしつ", Romaji = "kyoushitsu", Meaning = "Phòng học" },
                    new Vocabulary { NodeId = node4.Id, Word = "起きます", Hiragana = "おきます", Romaji = "okimasu", Meaning = "Thức dậy" },
                    new Vocabulary { NodeId = node5.Id, Word = "学校", Hiragana = "がっこう", Romaji = "gakkou", Meaning = "Trường học" }
                );

                // 5. Materials
                context.Materials.AddRange(
                    new Material
                    {
                        NodeId = node1.Id,
                        Title = "Video Bài 1: Ngữ pháp cơ bản N1 は N2 です",
                        MaterialType = "video",
                        Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                        Duration = 15
                    },
                    new Material
                    {
                        NodeId = node1.Id,
                        Title = "Luyện phát âm câu chào hỏi chuẩn người bản xứ",
                        MaterialType = "speech",
                        Url = "",
                        SpeechTargetText = "はじめまして、わたしはナムです。どうぞよろしくおねがいします。",
                        SpeechMeaning = "Rất vui được gặp bạn, tôi là Nam. Rất mong nhận được sự giúp đỡ của bạn."
                    }
                );

                // 6. Questions
                var q1 = new Question
                {
                    NodeId = node1.Id,
                    Content = "Điền trợ từ thích hợp vào chỗ trống: わたし ( ... ) がくせいです。",
                    QuestionType = "Single",
                    Category = "Ngữ pháp",
                    Visibility = "Always",
                    IsQuestionBank = true,
                    DifficultyLevel = 1,
                    Explanation = "Trợ từ は (đọc là wa) dùng để đánh dấu chủ ngữ trong câu khẳng định danh từ."
                };
                context.Questions.Add(q1);
                context.SaveChanges();

                context.QuestionOptions.AddRange(
                    new QuestionOption { QuestionId = q1.Id, OptionContent = "は (wa)", IsCorrect = true },
                    new QuestionOption { QuestionId = q1.Id, OptionContent = "が (ga)", IsCorrect = false },
                    new QuestionOption { QuestionId = q1.Id, OptionContent = "を (o)", IsCorrect = false },
                    new QuestionOption { QuestionId = q1.Id, OptionContent = "に (ni)", IsCorrect = false }
                );
                context.SaveChanges();
            }

            // ==========================================
            // 7. SEED CLASSES
            // ==========================================
            var class1 = context.Classes.FirstOrDefault(c => c.InviteCode == "JPN5K18");
            if (class1 == null)
            {
                class1 = new Class
                {
                    ClassName = "JP-N5-K18 (Sáng 2-4-6)",
                    ManagerId = managerUser.Id,
                    CurriculumId = curriculumN5.Id,
                    InviteCode = "JPN5K18",
                    Description = "Lớp học Tiếng Nhật N5 cơ bản cho người mới bắt đầu. Ca sáng thứ 2-4-6.",
                    Status = "Active",
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-15)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(75)),
                    CreatedAt = DateTime.Now.AddDays(-20)
                };
                context.Classes.Add(class1);
                context.SaveChanges();

                // Gán giáo viên và học viên vào lớp
                if (!context.ClassMembers.Any(cm => cm.ClassId == class1.Id))
                {
                    context.ClassMembers.AddRange(
                        new ClassMember { ClassId = class1.Id, UserId = teacher1.Id, JoinedAt = DateTime.Now.AddDays(-20), IsSupportTeam = false },
                        new ClassMember { ClassId = class1.Id, UserId = student1.Id, JoinedAt = DateTime.Now.AddDays(-18), IsSupportTeam = false },
                        new ClassMember { ClassId = class1.Id, UserId = student2.Id, JoinedAt = DateTime.Now.AddDays(-18), IsSupportTeam = false },
                        new ClassMember { ClassId = class1.Id, UserId = student3.Id, JoinedAt = DateTime.Now.AddDays(-17), IsSupportTeam = false }
                    );
                    context.SaveChanges();
                }

                // Mở khóa các bài học
                var n1 = context.Nodes.FirstOrDefault(n => n.CurriculumId == curriculumN5.Id);
                if (n1 != null && !context.ClassNodeStatuses.Any(cns => cns.ClassId == class1.Id && cns.NodeId == n1.Id))
                {
                    context.ClassNodeStatuses.Add(new ClassNodeStatus { ClassId = class1.Id, NodeId = n1.Id, IsUnlocked = true, UnlockedAt = DateTime.Now.AddDays(-15) });
                    context.SaveChanges();
                }
            }
        }
    }
}
