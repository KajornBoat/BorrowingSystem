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
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authentication;
using Firebase.Auth;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace LockLock.Controllers
{
    public class APIController : Controller
    {
        private readonly ILogger<APIController> _logger;
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;

        private FirebaseAuthProvider auth;

        private string[] rooms = { "HhJCxmYvz3PbhlelTeqm", "mJPKvyzMqzvO91tWZOYU", "ujDeZXlmtfO19cJaw9xz", "hc2hLRAwGTNakdpeuS0z", "BzWSgoSk9RAQNEIjwJlp" };
        public APIController(ILogger<APIController> logger)
        {
            _logger = logger;
            string projectId;
            using (StreamReader r = new StreamReader(firebaseJSON))
            {
                string json = r.ReadToEnd();
                var myJObject = JObject.Parse(json);
                projectId = myJObject.SelectToken("project_id").Value<string>();
            }
            firestoreDb = FirestoreDb.Create(projectId);

            auth = new FirebaseAuthProvider(
                            new FirebaseConfig("AIzaSyDYMUB0qohsGyFfdHCFWyxfcwr84HC-WCU"));
        }
        [HttpGet]
        public IActionResult Index()
        {
            return Content("sad");
        }

        [HttpGet]
        public async Task<IActionResult> table([FromQuery(Name = "room")] string roomQueryString)
        {
            string uid = await verifyTokenAsync();
            if (uid == null)
                return NotFound("User Error");
            if (!rooms.Contains(roomQueryString))
                return NotFound("Room Error");
            RoomModel Room = new RoomModel();
            Room.RoomID = roomQueryString;
            Console.WriteLine("RoomID => " + Room.RoomID);

            try
            {
                Console.WriteLine("yo");
                DocumentReference documentReference = firestoreDb.Collection("room").Document(Room.RoomID);
                Console.WriteLine("yo");
                DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                Console.WriteLine(documentSnapshot.Exists);

                RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                newRoom.RoomID = Room.RoomID;
                Room = newRoom;
            }
            catch
            {
                Console.WriteLine("in catch");
                return NotFound("Room Error");
            }

            DateTime timeRef = DateTime.Now.Date;
            DateTime timeNow = DateTime.Now.Date;
            timeNow = TimeZoneInfo.ConvertTimeToUtc(timeNow);
            DateTime timeEnd = timeNow.AddDays(7);
            Console.WriteLine("Now " + timeNow.ToString("u"));
            Console.WriteLine("Ref " + timeRef.ToString("u"));
            int hourNow = int.Parse(DateTime.Now.ToString("HH"));
            int dayNow = int.Parse(DateTime.Now.ToString("dd"));
            string timeLength = timeRef.ToString("dd MMMM") + " - " + timeRef.AddDays(6).ToString("dd MMMM yyyy");

            Query borrowQuery = firestoreDb.Collection("borrow").WhereGreaterThanOrEqualTo("time", timeNow).WhereLessThanOrEqualTo("time", timeEnd).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", false).WhereEqualTo("roomID", Room.RoomID);
            QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
            List<BorrowModel> listBorrow = new List<BorrowModel>();

            foreach (DocumentSnapshot documentSnapshot in borrowQuerySnapshot.Documents)
            {
                if (documentSnapshot.Exists)
                {
                    Dictionary<string, object> borrow = documentSnapshot.ToDictionary();
                    string timeTemp = borrow["time"].ToString().Replace("Timestamp:", "").Trim();
                    borrow["time"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(timeTemp.Remove(timeTemp.Length - 1, 1), "s", null), TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                    string json = JsonConvert.SerializeObject(borrow);
                    BorrowModel newBorrow = JsonConvert.DeserializeObject<BorrowModel>(json);
                    newBorrow.BorrowID = documentSnapshot.Id;
                    listBorrow.Add(newBorrow);
                }
            }

            uint[,] tableData = new uint[7, 9];
            for (int j = 0; j < 9; j++)
            {
                for (int i = 0; i < 7; i++)
                {
                    tableData[i, j] = 0;
                }
            }

            foreach (BorrowModel i in listBorrow)
            {
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
                if (hour < 18 && hour >= 9)
                {
                    tableData[x, hour - 9] = tableData[x, hour - 9] + 1;
                }
            }
            List<List<uint>> list = new List<List<uint>>();
            TableDataModel sendData = new TableDataModel();
            // sendData.roomID = Room.RoomID;
            sendData.time = timeLength;
            // sendData.timeDate = timeRef.Ticks;
            for (int j = 0; j < 9; j++)
            {
                List<uint> inList = new List<uint>();
                for (int i = 0; i < 7; i++)
                {
                    if (i == 0 && j <= hourNow - 9)
                    {
                        tableData[i, j] = 0;
                    }
                    else if (Room.objNum - tableData[i, j] <= 0)
                    {
                        tableData[i, j] = 0;
                    }
                    else
                    {
                        tableData[i, j] = Room.objNum - tableData[i, j];
                    }
                    inList.Add(tableData[i, j]);
                }
                list.Add(inList);
            }
            sendData.data = list;

            Console.WriteLine("finish");

            return Ok(sendData);
        }

        [HttpPost]
        public async Task<IActionResult> createTransaction([FromBody] TransactionAPIModel input)
        {
            string uid = await verifyTokenAsync();
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
                    return NotFound("UserError");
                }
            }
            else
            {
                return NotFound("UserError");
            }
            List<DateTime> timeList = new List<DateTime>();
            if (input.startDateTime.Minute != 0 && input.startDateTime.Second != 0)
                return NotFound("Date Error");
            for (int i = 0; i < input.hourPeriod; i++)
            {
                timeList.Add(input.startDateTime.AddHours(i));
            }

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
                return NotFound("RoomID Error");
            }

            bool isError = false;
            foreach (DateTime date in timeList)
            {
                Console.WriteLine(date.ToString());
                Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("time", TimeZoneInfo.ConvertTimeToUtc(date)).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", false).WhereEqualTo("roomID", Room.RoomID);
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
                return NotFound("DataError");
            }

            CollectionReference transactionCollection = firestoreDb.Collection("transaction");
            TransactionModel newTransaction = new TransactionModel()
            {
                roomID = input.roomID,
                timestamp = DateTime.UtcNow,
                userID = user.UserID,
                cancel = false
            };

            DocumentReference transactionDocument = await transactionCollection.AddAsync(newTransaction);
            string transactionId = transactionDocument.Id;

            foreach (DateTime date in timeList)
            {
                CollectionReference borrowCollection = firestoreDb.Collection("borrow");
                BorrowModel newBorrow = new BorrowModel()
                {
                    roomID = input.roomID,
                    time = date,
                    transactionID = transactionId,
                    cancel = false,
                    otherGroup = false
                };
                DocumentReference borrowDocument = await borrowCollection.AddAsync(newBorrow);
                Console.WriteLine(date);
            }

            return Ok("OK");

        }

        public async Task<IActionResult> allRoom()
        {
            string uid = await verifyTokenAsync();
            Query roomQuery = firestoreDb.Collection("room");
            QuerySnapshot roomQuerySnapshot = await roomQuery.GetSnapshotAsync();

            List<RoomModel> roomList = new List<RoomModel>();

            foreach (DocumentSnapshot value in roomQuerySnapshot)
            {
                RoomModel room = value.ConvertTo<RoomModel>();

                room.RoomID = value.Id;
                roomList.Add(room);
            }
            roomList = roomList.OrderBy(o => o.number).ToList();
            return Ok(roomList);
        }

        [HttpPost]
        public async Task<IActionResult> loginAsync([FromBody] loginRequest request)
        {
            try
            {
                var fbAuthLink = await auth.SignInWithEmailAndPasswordAsync(request.email, request.password);
                string token = fbAuthLink.FirebaseToken;
                //saving the token in a session variable
                if (token != null)
                {
                    return Ok(token);
                }
                else
                {
                    Console.Write("BadRequest");
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                Console.Write("BadRequest");
                return BadRequest();
            }
        }
        [HttpGet]
        public async Task<IActionResult> getAllAsync()
        {
            string token = await HttpContext.GetTokenAsync("Bearer", "access_token");
            if (token != null)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }

        }
        [HttpPost]
        public async Task<IActionResult> cancelTransaction([FromBody] cancelRequest request)
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(request.id);
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
                        return Ok();
                    }
                    else
                    {
                        return BadRequest("UserID not macth");
                    }

                }
                catch
                {
                    return BadRequest();
                }
            }
            else{
                return BadRequest();
            }
        }
        [HttpGet]
        public async Task<IActionResult> getTransactionByID(string id)
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(id);
                    DocumentSnapshot transactionSnapshot = await transactionReference.GetSnapshotAsync();
                    TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                    if (transactionData.userID != uid) return BadRequest("User ID not Macth");
                    if(transactionData.cancel) return BadRequest("This transaction is cancel");
                    
                    DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                    DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                    RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                    List<DateTime> timeLists = new List<DateTime>();
                    Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                    QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                    foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                    {
                        BorrowModel borrowData = borrowSnapshot.ConvertTo<BorrowModel>();
                        timeLists.Add(borrowData.time.ToLocalTime());
                    }
                    timeLists.Sort();

                    getTransaction transaction = new getTransaction()
                    {
                        reservation = new reservationData()
                        {
                            id = transactionSnapshot.Id,
                            userId = uid,
                            roomId = transactionData.roomID,
                            startDateTime = timeLists[0],
                            endDateTime = timeLists[timeLists.Count - 1].AddHours(1)
                        },
                        room = new roomReservation()
                        {
                            id = transactionData.roomID,
                            name = roomData.name,
                            equipmentName = roomData.objName
                        }
                    };
                    return Ok(transaction);
                }
                catch
                {
                    return BadRequest("Exception");
                }
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> getTransactionByUserAsync()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    List<getTransaction> bookingList = new List<getTransaction>();

                    Query transactionQuery = firestoreDb.Collection("transaction").WhereEqualTo("userID", uid).WhereEqualTo("cancel", false);
                    QuerySnapshot transactionQuerySnapshot = await transactionQuery.GetSnapshotAsync();

                    foreach (DocumentSnapshot transactionSnapshot in transactionQuerySnapshot)
                    {

                        if (transactionSnapshot.Exists)
                        {
                            TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                            DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                            DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                            RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                            List<DateTime> timeLists = new List<DateTime>();
                            Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                            QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                            foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                            {
                                BorrowModel borrowData = borrowSnapshot.ConvertTo<BorrowModel>();
                                timeLists.Add(borrowData.time.ToLocalTime());
                            }
                            timeLists.Sort();

                            getTransaction transaction = new getTransaction()
                            {
                                reservation = new reservationData()
                                {
                                    id = transactionSnapshot.Id,
                                    userId = uid,
                                    roomId = transactionData.roomID,
                                    startDateTime = timeLists[0],
                                    endDateTime = timeLists[timeLists.Count - 1].AddHours(1)
                                },
                                room = new roomReservation()
                                {
                                    id = transactionData.roomID,
                                    name = roomData.name,
                                    equipmentName = roomData.objName
                                }
                            };
                            bookingList.Add(transaction);
                        }
                        else
                        {
                            Console.WriteLine("Document does not exist!", transactionSnapshot.Id);
                        }
                    }
                    return Ok(bookingList);
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                    return BadRequest("Exception");
                }
            }
            else
            {
                return BadRequest();
            }
        }


        private async Task<string> verifyTokenAsync()
        {
            try
            {
                string authorizationHeader = this.HttpContext.Request.Headers["Authorization"];
                string token = authorizationHeader.Substring(7);
                FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                return decodedToken.Uid;
            }
            catch
            {
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
        }




    }
    public class TableDataModel
    {
        public List<List<uint>> data { get; set; }
        public string time { get; set; }
        // public string roomID { get; set; }
    }

    public class TransactionAPIModel
    {
        public string roomID { get; set; }
        public DateTime startDateTime { get; set; }
        public int hourPeriod { get; set; }
    }

    public class loginRequest
    {
        [Required]
        [JsonPropertyName("email")]
        public string email { get; set; }
        [Required]
        [JsonPropertyName("password")]
        public string password { get; set; }
    }
    public class cancelRequest
    {
        [Required]
        [JsonPropertyName("id")]
        public string id { get; set; }

    }
    public class reservationData
    {
        public string id { get; set; }
        public string userId { get; set; }
        public string roomId { get; set; }
        public DateTime startDateTime { get; set; }
        public DateTime endDateTime { get; set; }

    }
    public class roomReservation
    {
        public string id { get; set; }
        public string name { get; set; }
        public string equipmentName { get; set; }

    }
    public class getTransaction
    {
        public reservationData reservation { get; set; }
        public roomReservation room { get; set; }

    }


}