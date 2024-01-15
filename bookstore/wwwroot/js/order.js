let productDataTable;

$(document).ready(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const status = urlParams.get('status');

    loadDataTable(status);
});

const loadDataTable = (status) => {
    productDataTable = $('#tblData').DataTable({
        ajax: { url: `/admin/order/getall?status=${status}` },
        columns: [
            { data: 'id' },
            { data: 'name' },
            { data: 'phoneNumber' },
            { data: 'applicationUser.email' },
            { data: 'orderStatus' },
            { data: 'orderTotal' },
            {
                data: 'id',
                render: (data) => `
                <div class="w-75 btn-group" role="group">
                <a href="/Admin/Order/details?entityId=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                <div>
            `,
            },
        ],
    });
};
