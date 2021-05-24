using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using LockLock.Models;
using Newtonsoft.Json;
using FirebaseAdmin.Auth;

using Newtonsoft.Json.Converters;


namespace LockLock.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private string projectId;
        private FirestoreDb firestoreDb;

        private string[] rooms = { "HhJCxmYvz3PbhlelTeqm", "mJPKvyzMqzvO91tWZOYU", "ujDeZXlmtfO19cJaw9xz", "hc2hLRAwGTNakdpeuS0z", "BzWSgoSk9RAQNEIjwJlp" };



        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseJSON);
            projectId = "locklock-47b1d";
            firestoreDb = FirestoreDb.Create(projectId);
        }

        private async Task<string> checkLogedIn()
        {
            var token = HttpContext.Session.GetString("_UserToken");
            if (token != null)
            {
                try
                {
                    FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    Console.WriteLine(decodedToken.Uid);
                    return decodedToken.Uid;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return null;
            }
        }

        public async Task<IActionResult> Index([FromQuery(Name = "room")] int roomQueryString)
        {
            string uid = await checkLogedIn();
            UserModel user = new UserModel();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);
                    Console.WriteLine(uid);

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    user = newUser;
                    if (user.role == "admin")
                        return RedirectToAction("Index", "Admin");
                }
                catch
                {
                    return RedirectToAction("SignIn", "Account");
                }
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
            Console.WriteLine("UserID = " + user.UserID);
            Console.WriteLine("Firstname = " + user.Firstname);
            Console.WriteLine("Lastname = " + user.Lastname);
            Console.WriteLine("Email = " + user.Email);
            Console.WriteLine("Tel = " + user.Tel);

            BlacklistDataModel blacklist = new BlacklistDataModel();
            blacklist.adminID = "";
            try
            {
                Query blacklistQuery = firestoreDb.Collection("blacklist").WhereEqualTo("userID", user.UserID);
                QuerySnapshot blacklistQuerySnapshot = await blacklistQuery.GetSnapshotAsync();

                foreach (DocumentSnapshot documentSnapshot in blacklistQuerySnapshot.Documents)
                {
                    if (documentSnapshot.Exists)
                    {
                        BlacklistDataModel newBlacklist = documentSnapshot.ConvertTo<BlacklistDataModel>();
                        newBlacklist.BlacklistID = documentSnapshot.Id;
                        blacklist = newBlacklist;
                        Console.WriteLine(blacklist.adminID);
                    }
                    else
                    {
                        blacklist.adminID = "";
                        Console.WriteLine("else");
                    }
                }
            }
            catch
            {
                blacklist.adminID = "";
                Console.WriteLine("catch");
            }
            if (blacklist.adminID == "")
            {

                int roomNum = 0;
                roomNum = roomQueryString;
                Console.WriteLine("QueryString => " + roomNum);
                RoomModel Room = new RoomModel();
                Room.RoomID = rooms[roomNum == 0 ? 0 : roomNum - 1];
                Console.WriteLine("RoomID => " + Room.RoomID);

                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("room").Document(Room.RoomID);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);

                    RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                    newRoom.RoomID = Room.RoomID;
                    Room = newRoom;
                }
                catch
                {
                    return RedirectToAction(nameof(Error));
                }

                // string roomID = "Room 1";
                // Query roomQuery = firestoreDb.Collection("room").WhereEqualTo("name", roomID);
                // QuerySnapshot roomQuerySnapshot = await roomQuery.GetSnapshotAsync();
                // // List<RoomModel> listRoom = new List<RoomModel>();
                // RoomModel Room = new RoomModel();

                // foreach (DocumentSnapshot documentSnapshot in roomQuerySnapshot)
                // {
                //     if (documentSnapshot.Exists)
                //     {
                //         Dictionary<string, object> room = documentSnapshot.ToDictionary();
                //         string json = JsonConvert.SerializeObject(room);
                //         RoomModel newRoom = JsonConvert.DeserializeObject<RoomModel>(json);
                //         newRoom.RoomID = documentSnapshot.Id;
                //         Room = newRoom;
                //         // listRoom.Add(newRoom);
                //     }
                // }

                // RoomModel thisRoom = Room;//listRoom[0]
                Console.WriteLine("RoomID = " + Room.RoomID);
                Console.WriteLine("adminID = " + Room.adminID);
                Console.WriteLine("name = " + Room.name);
                Console.WriteLine("objName = " + Room.objName);
                Console.WriteLine("objNum = " + Room.objNum);
                // foreach (RoomModel i in listRoom)
                // {
                //     Console.WriteLine("RoomID = " + i.RoomID);
                //     Console.WriteLine("adminID = " + i.adminID);
                //     Console.WriteLine("name = " + i.name);
                //     Console.WriteLine("objName = " + i.objName);
                //     Console.WriteLine("objNum = " + i.objNum);
                // }

                // get room data from Game API here //
                DateTime timeRef = DateTime.Now.Date;
                DateTime timeNow = DateTime.Now.Date;
                timeNow = TimeZoneInfo.ConvertTimeToUtc(timeNow);
                // timeNow = TimeZoneInfo.ConvertTimeFromUtc(timeNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                DateTime timeEnd = timeNow.AddDays(7);
                Console.WriteLine("Now " + timeNow.ToString("u"));
                Console.WriteLine("Ref " + timeRef.ToString("u"));
                int hourNow = int.Parse(DateTime.Now.ToString("HH"));
                int dayNow = int.Parse(DateTime.Now.ToString("dd"));
                // Console.WriteLine("hour Now " + hourNow);
                string timeLength = timeRef.ToString("dd MMMM") + " - " + timeRef.AddDays(6).ToString("dd MMMM yyyy");
                List<string> viewDataName = new List<string>();
                for (int i = 0; i < 7; i++)
                {
                    viewDataName.Add(timeRef.AddDays(i).ToString("ddd"));
                }
                // foreach (string i in viewDataName)
                // {
                //     Console.WriteLine(i);
                // }
                // Console.WriteLine(timeNow.ToString("dd MMMM") + " - " + timeNow.AddDays(6).ToString("dd MMMM yyyy"));
                // Console.WriteLine(timeNow.AddDays(6).ToString("dd MMMM yyyy"));

                Query borrowQuery = firestoreDb.Collection("borrow").WhereGreaterThanOrEqualTo("time", timeNow).WhereLessThanOrEqualTo("time", timeEnd).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", false).WhereEqualTo("roomID", Room.RoomID);
                QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                List<BorrowModel> listBorrow = new List<BorrowModel>();

                foreach (DocumentSnapshot documentSnapshot in borrowQuerySnapshot.Documents)
                {
                    // Console.WriteLine("hello");
                    // Console.WriteLine(documentSnapshot.Exists);
                    if (documentSnapshot.Exists)
                    {
                        Dictionary<string, object> borrow = documentSnapshot.ToDictionary();
                        string timeTemp = borrow["time"].ToString().Replace("Timestamp:", "").Trim();
                        borrow["time"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(timeTemp.Remove(timeTemp.Length - 1, 1), "s", null), TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                        // borrow["time"] = DateTime.ParseExact(timeTemp.Remove(timeTemp.Length - 1, 1), "s", null);
                        // Console.WriteLine(borrow["time"]);
                        // foreach (KeyValuePair<string, object> kvp in borrow)
                        // {
                        //     //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                        //     Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                        // }
                        string json = JsonConvert.SerializeObject(borrow);
                        // Console.WriteLine(json);
                        BorrowModel newBorrow = JsonConvert.DeserializeObject<BorrowModel>(json);
                        newBorrow.BorrowID = documentSnapshot.Id;
                        listBorrow.Add(newBorrow);
                    }
                }
                // foreach (BorrowModel i in listBorrow)
                // {
                //     Console.WriteLine("BorrowID = " + i.BorrowID);
                //     Console.WriteLine("roomID = " + i.roomID);
                //     Console.WriteLine("time = " + i.time);
                //     Console.WriteLine("userID = " + i.transactionID);
                // }


                Tuple<string, uint>[,] tableData = new Tuple<string, uint>[7, 9];
                for (int j = 0; j < 9; j++)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        tableData[i, j] = new Tuple<string, uint>("", 0);
                    }
                }
                // tableData[1, 0] = new Tuple<string, uint>("Green", 1);
                // for (int j = 0; j < 9; j++)
                // {
                //     for (int i = 0; i < 7; i++)
                //     {
                //         Console.Write(tableData[i, j].Item1 + "-" + tableData[i, j].Item2 + " ");
                //     }
                //     Console.WriteLine();
                // }
                // List<List<Tuple<string, uint>>> viewDataTable = new List<List<Tuple<string, uint>>>();
                // List<Tuple<string, uint>> templateList = new List<Tuple<string, uint>>();
                // for (int i = 0; i < 9; i++)
                // {
                //     templateList.Add(new Tuple<string, uint>("", 0));
                // }
                // for (int i = 0; i < 7; i++)
                // {
                //     viewDataTable.Add(templateList);
                // }

                // Console.WriteLine(viewDataTable[1][0].Item1 + " " + viewDataTable[1][0].Item2);

                foreach (BorrowModel i in listBorrow)
                {
                    // Console.WriteLine(i.time.Subtract(timeRef).ToString());//.Split(".")[0]
                    int day = int.Parse(i.time.ToString("dd"));
                    int hour = int.Parse(i.time.ToString("HH"));
                    int x;
                    if (day == dayNow)
                    {
                        x = 0;
                    }
                    else
                    {
                        x = int.Parse(i.time.Subtract(timeRef).ToString().Split(".")[0]);
                    }
                    // Console.WriteLine(day + " " + hour + " " + dayNow + " " + hourNow);
                    if (hour < 18 && hour >= 9) // !(int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0]) == 0 && hour - hourNow <= 0) && 
                    {
                        // Console.WriteLine(viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item1 + " " + viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item2);

                        // List<Tuple<string, uint>> temp = viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])];
                        // for (int j = 0; j < 9; j++)
                        // {
                        //     Console.WriteLine(temp[j]);
                        // }
                        // temp[hour - 9] = new Tuple<string, uint>(viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item1, viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item2 + 1);
                        tableData[x, hour - 9] = new Tuple<string, uint>(tableData[x, hour - 9].Item1, tableData[x, hour - 9].Item2 + 1);
                    }
                }

                for (int j = 0; j < 9; j++)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (i == 0 && j <= hourNow - 9)
                        {
                            tableData[i, j] = new Tuple<string, uint>("Grey", Room.objNum - tableData[i, j].Item2);
                        }
                        else if (Room.objNum - tableData[i, j].Item2 <= 0)
                        {
                            if (j % 2 == 0)
                                tableData[i, j] = new Tuple<string, uint>("Yellow", Room.objNum - tableData[i, j].Item2);
                            else
                                tableData[i, j] = new Tuple<string, uint>("Red", Room.objNum - tableData[i, j].Item2);
                        }
                        else
                        {
                            tableData[i, j] = new Tuple<string, uint>("Green", Room.objNum - tableData[i, j].Item2);
                        }
                    }
                }

                // for (int j = 0; j < 9; j++)
                // {
                //     for (int i = 0; i < 7; i++)
                //     {
                //         Console.Write(tableData[i, j].Item1 + "-" + tableData[i, j].Item2 + " ");
                //     }
                //     Console.WriteLine();
                // }

                // for (int j = 0; j < 9; j++)
                // {
                //     for (int i = 0; i < 7; i++)
                //     {
                //         Console.Write(viewDataTable[i][j].Item1 + "-" + viewDataTable[i][j].Item2 + " ");
                //     }
                //     Console.WriteLine();
                // }

                TableModel viewData = new TableModel()
                {
                    objName = Room.objName,
                    timeLength = timeLength,
                    name = viewDataName,
                    table = tableData,
                    firstName = user.Firstname,
                    lastName = user.Lastname,
                    roomName = Room.name,
                    roomID = Room.RoomID,
                    roomNum = Array.IndexOf(rooms, Room.RoomID) + 1,
                    adminEmail = ""
                };

                // listBorrow.ForEach(Console.WriteLine);
                // Console.WriteLine(listBorrow);
                return View(viewData);
            }
            else
            {
                UserModel admin = new UserModel();
                try
                {
                    Console.WriteLine(blacklist.adminID);
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(blacklist.adminID);
                    Console.WriteLine("news");
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine("newss");
                    Console.WriteLine(documentSnapshot.Exists);
                    Console.WriteLine(uid);

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    admin = newUser;
                }
                catch
                {
                    return RedirectToAction(nameof(Error));
                }
                int roomNum = 0;
                roomNum = roomQueryString;
                RoomModel Room = new RoomModel();
                Room.RoomID = rooms[roomNum == 0 ? 0 : roomNum - 1];
                TableModel viewData = new TableModel()
                {
                    adminEmail = admin.Email,
                    firstName = user.Firstname,
                    lastName = user.Lastname,
                    roomName = Room.name,
                    roomID = Room.RoomID,
                    roomNum = Array.IndexOf(rooms, Room.RoomID) + 1,
                };
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("room").Document(Room.RoomID);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);

                    RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                    newRoom.RoomID = Room.RoomID;
                    Room = newRoom;
                }
                catch
                {
                    return RedirectToAction(nameof(Error));
                }
                return View(viewData);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Transaction([FromBody] CreateTModel input)//public async Task<IActionResult> Transaction(string roomID, List<string> dates)
        {
            string uid = await checkLogedIn();
            UserModel user = new UserModel();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    user = newUser;
                }
                catch
                {
                    return StatusCode(400, "UserError");
                }
            }
            else
            {
                return StatusCode(400, "UserError");
            }

            Console.WriteLine();
            Console.WriteLine("UserID = " + user.UserID);
            Console.WriteLine("Firstname = " + user.Firstname);
            Console.WriteLine("Lastname = " + user.Lastname);
            Console.WriteLine("Email = " + user.Email);
            Console.WriteLine("Tel = " + user.Tel);
            // Console.WriteLine("RoomID = " + roomID);

            Console.WriteLine("RoomID = " + input.roomID);
            int dateCheck = -1;
            bool dateBool = false;
            foreach (string date in input.dates)
            {
                string[] temp = date.Split(" ");
                int tempI = int.Parse(temp[1]);
                if (dateCheck == -1)
                {
                    dateCheck = tempI;
                }
                else if (tempI != dateCheck)
                {
                    dateBool = true;
                }
                Console.WriteLine(date);
            }

            if (dateCheck == -1 || dateBool)
            {
                return StatusCode(400, "DataError");
            }

            // check data in DB
            RoomModel Room = new RoomModel();
            try
            {
                DocumentReference documentReference = firestoreDb.Collection("room").Document(input.roomID);
                DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                Console.WriteLine(documentSnapshot.Exists);

                RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                newRoom.RoomID = input.roomID;
                Room = newRoom;
            }
            catch
            {
                return StatusCode(400, "DataError");
            }
            bool isError = false;
            foreach (string date in input.dates)
            {
                string[] temp = date.Split(" ");
                string[] month = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

                DateTime timeCheck = new DateTime(int.Parse(temp[3]), Array.IndexOf(month, temp[2]) + 1, int.Parse(temp[1]), int.Parse(temp[4].Split(".")[0]), 0, 0);

                Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("time", TimeZoneInfo.ConvertTimeToUtc(timeCheck)).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", false).WhereEqualTo("roomID", Room.RoomID);
                QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                int count = 0;

                foreach (DocumentSnapshot documentSnapshot in borrowQuerySnapshot.Documents)
                {
                    if (documentSnapshot.Exists)
                    {
                        count++;
                    }
                }
                Console.WriteLine(count);
                if (count >= Room.objNum)
                {
                    isError = true;
                }
            }
            if (isError)
            {
                return StatusCode(400, "DataError");
            }

            // store data in DB
            CollectionReference transactionCollection = firestoreDb.Collection("transaction");
            TransactionModel newTransaction = new TransactionModel()
            {
                roomID = input.roomID,
                timestamp = DateTime.UtcNow,
                userID = user.UserID,
                cancel = false
            };
            Console.WriteLine("Yahoo!");

            DocumentReference transactionDocument = await transactionCollection.AddAsync(newTransaction);
            string transactionId = transactionDocument.Id;

            // CollectionReference borrowCollection = firestoreDb.Collection("borrow");
            // BorrowModel newBorrow = new BorrowModel();

            // Console.WriteLine(roomID);

            foreach (string date in input.dates)
            {
                string[] temp = date.Split(" ");
                string[] month = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

                DateTime save = new DateTime(int.Parse(temp[3]), Array.IndexOf(month, temp[2]) + 1, int.Parse(temp[1]), int.Parse(temp[4].Split(".")[0]), 0, 0);

                CollectionReference borrowCollection = firestoreDb.Collection("borrow");
                BorrowModel newBorrow = new BorrowModel()
                {
                    roomID = input.roomID,
                    time = TimeZoneInfo.ConvertTimeToUtc(save),
                    transactionID = transactionId,
                    cancel = false,
                    otherGroup = false
                };
                DocumentReference borrowDocument = await borrowCollection.AddAsync(newBorrow);
                Console.WriteLine(date);
            }
            // return RedirectToAction("History", "Home");
            return Ok(Json("OK"));
        }

        [HttpPost]
        public IActionResult Test()
        {
            return StatusCode(404, "UserError");
            // return Ok(Json("OK"));
        }

        public async Task<IActionResult> History()
        {
            string uid = await checkLogedIn();

            if (uid != null)
            {
                UserModel user = new UserModel();
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);
                    Console.WriteLine(uid);

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    user = newUser;
                    TempData["name"] = user.Firstname;
                    TempData["surname"] = user.Lastname;
                }
                catch
                {
                    return RedirectToAction("SignIn", "Account");
                }

                List<BookingModel> bookingList = new List<BookingModel>();

                Query transactionQuery = firestoreDb.Collection("transaction").WhereEqualTo("userID", uid).WhereEqualTo("cancel", false);
                QuerySnapshot transactionQuerySnapshot = await transactionQuery.GetSnapshotAsync();

                DateTime currentDate = DateTime.Now;

                foreach (DocumentSnapshot transactionSnapshot in transactionQuerySnapshot)
                {

                    if (transactionSnapshot.Exists)
                    {
                        TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                        Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                        QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                        List<DateTime> timeLists = new List<DateTime>();
                        foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                        {
                            BorrowModel borrowData = borrowSnapshot.ConvertTo<BorrowModel>();
                            timeLists.Add(borrowData.time.ToLocalTime());
                        }
                        timeLists.Sort();
                        int timeCompare = DateTime.Compare(timeLists[0].AddHours(-1), currentDate);

                        DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                        DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                        RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                        BookingModel bookingItem = new BookingModel()
                        {
                            BookingID = transactionSnapshot.Id,
                            Name = roomData.objName,
                            Num = 1,
                            RoomName = roomData.name,
                            timeList = timeLists,
                            cancel = timeCompare > 0 ? true : false,
                            timestamp = transactionData.timestamp
                        };
                        bookingList.Add(bookingItem);
                    }
                    else
                    {
                        Console.WriteLine("Document does not exist!", transactionSnapshot.Id);
                    }
                }
                bookingList = bookingList.OrderBy(x => x.timestamp).ToList();
                return View(bookingList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }


        public async Task<IActionResult> cancelAsync(string transactionID)
        {
            string uid = await checkLogedIn();
            if (uid != null)
            {
                DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(transactionID);
                DocumentSnapshot transactionSnapshot = await transactionReference.GetSnapshotAsync();

                TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                if (transactionData.userID == uid)
                {
                    await transactionReference.UpdateAsync("cancel", true);

                    Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                    QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                    foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                    {
                        DocumentReference borrowReference = firestoreDb.Collection("borrow").Document(borrowSnapshot.Id);
                        await borrowReference.UpdateAsync("cancel", true);
                    }
                    return RedirectToAction("History", "Home");
                }
                else
                {
                    Console.WriteLine("UserID not macth");
                    return RedirectToAction("History", "Home");
                }


            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }
        public IActionResult Blacklist()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult EditProfile()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult SignIn()
        {
            return View();
        }

        public IActionResult MainRoom()
        {
            return View();
        }

        public IActionResult Edit()
        {
            return View();
        }
        
         public IActionResult HistoryAdmin()
        {
            return View();
        }
        
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
