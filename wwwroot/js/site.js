// JWT 토큰을 세션 스토리지에서 가져오는 함수
function getAuthorizationHeader() {
    const token = sessionStorage.getItem('jwtToken');
    return token ? `Bearer ${token}` : null;
}

// 모든 요청에 대해 Authorization 헤더를 추가
// url과 option을 매개변수로 받음
function fetchWithAuth(url, options = {}) {
    const headers = new Headers(options.headers || {});
    const authHeader = getAuthorizationHeader();

    if (authHeader) {
        headers.append('Authorization', authHeader);
    }

    // JavaScript의 객체 확산 연산자(...)를 사용하여 
    // options 객체의 모든 속성을 새로운 객체에 복사한 다음, 
    // headers 속성을 추가하거나 덮어씀.
    return fetch(url, {
        ...options,
        headers: headers
    });
}


// GET 요청
function getMemberIndex() {
    fetchWithAuth('/api/Home/MemberIndex')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
        });
}

// POST 요청 예제 함수
function postMemberIndex(postData) {
    fetchWithAuth('/api/MemberIndex', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(postData)
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            console.log('서버 응답:', data);
        })
        .catch(error => {
            console.error('Fetch 오류:', error);
        });
}

function getMemberIndex() {
    fetchWithAuth('/api/MemberIndex')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            console.log('서버 응답:', data);
        })
        .catch(error => {
            console.error('Fetch 오류:', error);
        });
}