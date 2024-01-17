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

                    const links = `
                    <div class="w-75 btn-group" role="group">
                    <a onClick="LockUser('/Admin/User/LockUser', '${
                        data.id
                    }', '${data.lockoutEnd}');" class="btn btn-${
                        userLocked ? 'success' : 'danger'
                    } mx-2"><i class="bi bi-${
                        userLocked ? 'unlock-fill' : 'lock-fill'
                    }"></i> ${userLocked ? 'Unlock' : 'Lock'}</a>
                    <a class="btn btn-danger mx-2"><i class="bi bi-pencil-square"></i> Permission</a>
                    </div>
                    `;

                    return links;
                },
            },
        ],
    });
};

const LockUser = (url, entityId, lockoutEnd) => {
    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            entityId,
            lockoutEnd,
        }),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                userDataTable.ajax.reload();
                toastr.success(data.message);
            } else {
                toastr.error('Something went worng. Please try again.');
            }
        })
        .catch((error) => {
            toastr.error(error.message);
        });
};
