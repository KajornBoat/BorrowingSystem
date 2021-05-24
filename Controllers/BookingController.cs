using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class BookingController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;
        public BookingController()
        {
            string projectId;
            using (StreamReader r = new StreamReader(firebaseJSON))
            {
                string json = r.ReadToEnd();
                var myJObject = JObject.Parse(json);
                projectId = myJObject.SelectToken("project_id").Value<string>();
            }
            firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task<IActionResult> IndexAsync()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
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
                            cancel = timeCompare > 0 ? true : false
                        };
                        bookingList.Add(bookingItem);
                    }
                    else
                    {
                        Console.WriteLine("Document does not exist!", transactionSnapshot.Id);
                    }
                }
                return View(bookingList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }

        }

        public async Task<IActionResult> cancelAsync(string transactionID)
        {
            string uid = await verifyTokenAsync();
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
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    Console.WriteLine("UserID not macth");
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }

        private async Task<string> verifyTokenAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("_UserToken");
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
}
