var modal;
var modalClose;

window.onload = function () {
  modal = document.getElementById('CancelModal');
  //var modalButton = document.getElementById('btnCancle');
  modalClose = document.getElementById('modalClose');
  console.log(modal);
  console.log(modalClose);

  modalClose.onclick = function () {
    modal.style.display = 'none';
    console.log('clo');
  };

  window.onclick = function () {
    if (event.target == modal) {
      modal.style.display = 'none';
      console.log('out');
    }
  };
};

function toModal(id) {
  //   var id = this.data-toID;
  console.log(id);
  var form = document.getElementById('cancelForm');
  form.setAttribute('action', `/Blacklist/cancel?id=${id}`);
  modal.style.display = 'block';
  console.log('but');
}

function checkString(x, y, z) {
  if (z == 'asc') return x.innerHTML.toLowerCase() > y.innerHTML.toLowerCase();
  else return x.innerHTML.toLowerCase() < y.innerHTML.toLowerCase();
}
function checkInt(x, y, z) {
  if (z == 'asc') return Number(x.innerHTML) > Number(y.innerHTML);
  else return Number(x.innerHTML) < Number(y.innerHTML);
}
function checkDate(x, y, z) {
  var months = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ];
  var a = x.innerHTML;
  var b = y.innerHTML;
  var temp1 = a.split(' ');
  var temp2 = b.split(' ');
  var checkX = new Date(
    parseInt(temp1[3]),
    months.indexOf(temp1[2]),
    parseInt(temp1[1]),
    parseInt(temp1[4])
  );
  var checkY = new Date(
    parseInt(temp2[3]),
    months.indexOf(temp2[2]),
    parseInt(temp2[1]),
    parseInt(temp2[4])
  );
  if (z == 'asc') return checkX.getTime() > checkY.getTime();
  else return checkX.getTime() < checkY.getTime();
}
function sortTable(n) {
  var table,
    rows,
    switching,
    i,
    x,
    y,
    shouldSwitch,
    dir,
    switchcount = 0;
  table = document.getElementById('blacklistTable');
  switching = true;
  //Set the sorting direction to ascending:
  dir = 'asc';
  /*Make a loop that will continue until
    no switching has been done:*/
  while (switching) {
    //start by saying: no switching is done:
    switching = false;
    rows = table.rows;
    /*Loop through all table rows (except the
      first, which contains table headers):*/
    for (i = 1; i < rows.length - 1; i++) {
      //start by saying there should be no switching:
      shouldSwitch = false;
      /*Get the two elements you want to compare,
        one from current row and one from the next:*/
      x = rows[i].getElementsByTagName('TD')[n];
      y = rows[i + 1].getElementsByTagName('TD')[n];
      /*check if the two rows should switch place,
        based on the direction, asc or desc:*/
      if (dir == 'asc') {
        if (n == 0) {
          if (checkInt(x, y, 'asc')) {
            //if so, mark as a switch and break the loop:
            shouldSwitch = true;
            break;
          }
        } else if (checkString(x, y, 'asc')) {
          //if so, mark as a switch and break the loop:
          shouldSwitch = true;
          break;
        }
      } else if (dir == 'desc') {
        if (n == 0) {
          if (checkInt(x, y, 'desc')) {
            //if so, mark as a switch and break the loop:
            shouldSwitch = true;
            break;
          }
        } else if (checkString(x, y, 'desc')) {
          //if so, mark as a switch and break the loop:
          shouldSwitch = true;
          break;
        }
      }
    }
    if (shouldSwitch) {
      /*If a switch has been marked, make the switch
        and mark that a switch has been done:*/
      rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
      switching = true;
      //Each time a switch is done, increase this count by 1:
      switchcount++;
    } else {
      /*If no switching has been done AND the direction is "asc",
        set the direction to "desc" and run the while loop again.*/
      if (switchcount == 0 && dir == 'asc') {
        dir = 'desc';
        switching = true;
      }
    }
  }
  var top = table.getElementsByTagName('TH');
  for (var i = 0; i < top.length; i++) {
    var temp = table.getElementsByTagName('TH')[i].innerHTML.split(' ')[0];
    if (n == i) {
      if (dir == 'asc') {
        table.getElementsByTagName('TH')[i].innerHTML = temp + ' &#9662;';
      } else {
        table.getElementsByTagName('TH')[i].innerHTML = temp + ' &#9652;';
      }
    } else {
      table.getElementsByTagName('TH')[i].innerHTML = temp;
    }
  }
}
