function jsRequest() {
  fetch('/User/changePassword', {
    method: 'GET',
  }).then((response) => {
    if (response.ok) {
      alert('สามารถเปลี่ยน password ได้ในกล่องจดหมายของ e-mail ของคุณ');
    } else {
      alert('System error');
    }
  });
}
