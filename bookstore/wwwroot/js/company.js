let companyDataTable;

$(document).ready(() => loadDataTable());

const loadDataTable = () => {
    companyDataTable = $('#tblData').DataTable({
        ajax: { url: '/admin/company/getall' },
        columns: [
            { data: 'name' },
            { data: 'streetAddress' },
            { data: 'city' },
            { data: 'state' },
            { data: 'postalCode' },
            { data: 'phoneNumber' },
            {
                data: 'id',
                render: (data) => `
                <div class="w-75 btn-group" role="group">
                <a href="/Admin/Company/Upsert?entityId=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i> Edit</a>
                <a onClick="Delete('/Admin/Company/Delete', ${data});" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i> Delete</a>
                <div>
            `,
            },
        ],
    });
};

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
                        companyDataTable.ajax.reload();
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
