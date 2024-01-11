const Delete = (url, id) => {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!',
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(url, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ entityId: id }),
            })
                .then((res) => res.json())
                .then((data) => {
                    if (data.success) {
                        productDataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error('Something went worng. Please try again.');
                    }
                })
                .catch((error) => {
                    toastr.error(error.message);
                });
        }
    });
};
