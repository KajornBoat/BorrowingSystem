function changeBut(i) {
  var submit = document.getElementById(`submit${i}`);
  var change = document.getElementById(`change${i}`);
  var objNum = document.getElementById(`objNum${i}`);
  var objName = document.getElementById(`objName${i}`);

  objNum.disabled = false;
  objName.disabled = false;
  submit.style.display = 'block';
  change.style.display = 'none';
}
