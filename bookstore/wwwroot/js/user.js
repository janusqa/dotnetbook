let userDataTable;

$(document).ready(() => loadDataTable());

const loadDataTable = () => {
    userDataTable = $('#tblData').DataTable({
        ajax: { url: '/admin/user/getall' },
        columns: [
            { data: 'name' },
            { data: 'email' },
            { data: 'phoneNumber' },
            { data: 'company.name' },
            { data: 'roleName' },
            {
                data: { id: 'id', lockoutEnd: 'lockoutEnd' },
                render: (data) => {
                    const today = new Date().getTime();
                    const lockout =
                        data.lockoutEnd != null
                            ? new Date(data.lockoutEnd).getTime()
                            : null;

                    const userLocked =
                        lockout == null || lockout <= today ? false : true;

                    const payload = {
                        test1: 1,
                        test2: 2,
                    };

                    const links = `;
                    <div class="w-75 btn-group" role="group">
                    <a onClick="Lock('/Admin/User/Lock', ${JSON.stringify(
                        payload
                    )
                        .split("'")
                        .join('&quot;')});" id="lock"  class="btn btn-${
                        userLocked ? 'success' : 'danger'
                    } mx-2"><i class="bi bi-${
                        userLocked ? 'unlock-fill' : 'lock-fill'
                    }"></i> ${userLocked ? 'Unlock' : 'Lock'}</a>
                    <a  id="permission" class="btn btn-danger mx-2"><i class="bi bi-pencil-square"></i> Permission</a>
                    </div>
                    `;

                    return links;
                },
            },
        ],
    });
};

const Lock = (url, data) => {
    console.log(data);
    // fetch(url, {
    //     method: 'POST',
    //     headers: { 'Content-Type': 'application/json' },
    //     body: JSON.stringify({
    //         entityId: data.id,
    //         lockoutEnd: data.lockoutEnd,
    //     }),
    // })
    //     .then((res) => res.json())
    //     .then((data) => {
    //         if (data.success) {
    //             userDataTable.ajax.reload();
    //             toastr.success(data.message);
    //         } else {
    //             toastr.error('Something went worng. Please try again.');
    //         }
    //     })
    //     .catch((error) => {
    //         toastr.error(error.message);
    //     });
};
