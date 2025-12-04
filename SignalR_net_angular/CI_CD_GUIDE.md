# 📘 Hướng dẫn chi tiết CI/CD Pipeline

Tài liệu này mô tả chi tiết từng bước của quy trình CI/CD (Continuous Integration/Continuous Deployment) trong dự án **SignalR Real-time Notification System**.

---

## 📋 Tổng quan

Dự án có **4 pipeline CI/CD** khác nhau tùy theo môi trường và nhu cầu:

1. **Jenkinsfile.windows** - Pipeline cho Windows Jenkins Agent (nhánh main)
2. **Jenkinsfile** (root) - Pipeline cho Linux Jenkins Agent, nhánh main (có push Docker Hub)
3. **Jenkinsfile.develop** - Pipeline cho Linux Jenkins Agent, nhánh develop (không push Docker Hub)
4. **SignalR_net_angular/Jenkinsfile** - Pipeline đơn giản (chỉ build và deploy)

---

## 🔧 Yêu cầu hệ thống

### Jenkins Server
- **Jenkins 2.0+** với các plugin:
  - Pipeline
  - Git
  - GitHub (để nhận webhook)
  - Docker Pipeline (nếu cần)

### Jenkins Agent
- **Windows Agent** (cho Jenkinsfile.windows):
  - .NET 8.0 SDK
  - Node.js 18+ và npm
  - Docker Desktop hoặc Docker Engine
  - Git

- **Linux Agent** (cho Jenkinsfile):
  - .NET 8.0 SDK
  - Node.js 18+ và npm
  - Docker Engine
  - Git

### External Services
- **GitHub Repository**: `https://github.com/ntdno1/LearnRabitMQ.git`
- **Docker Hub** (tùy chọn, cho Jenkinsfile root)
- **RabbitMQ Server**: `47.130.33.106:5672`

---

## 🚀 Pipeline 1: Jenkinsfile.windows (Windows Agent)

### Mục đích
Pipeline này được thiết kế để chạy trên **Windows Jenkins Agent**, build và deploy ứng dụng **cục bộ** (không push lên Docker Hub).

### Cấu trúc Pipeline

```
Checkout → Verify merge target → Backend Build → Frontend Build → Docker Build → Deploy
```

### Chi tiết từng Stage

#### **Stage 1: Checkout**
```groovy
stage('Checkout') {
    steps {
        git branch: 'main', url: 'https://github.com/ntdno1/LearnRabitMQ.git'
        script {
            env.GIT_COMMIT_SHORT = bat(...)  // Lấy 7 ký tự đầu của commit hash
            env.IMAGE_TAG = "${GIT_COMMIT_SHORT}-${env.BUILD_NUMBER}"
        }
    }
}
```

**Mục đích:**
- Clone code từ GitHub nhánh `main`
- Tạo image tag dựa trên commit hash và build number (ví dụ: `2fd8587-15`)

**Kết quả:**
- Code được checkout vào workspace Jenkins
- Biến môi trường `IMAGE_TAG` được tạo

---

#### **Stage 2: Verify merge target** (Optional)
```groovy
stage('Verify merge target') {
    when {
        changeRequest(target: 'main')
    }
    steps {
        echo "Change Request nhắm tới nhánh main..."
    }
}
```

**Mục đích:**
- Chỉ chạy khi có Pull Request/Change Request nhắm tới nhánh `main`
- Xác nhận rằng pipeline đang xử lý merge request

---

#### **Stage 3: Backend • Restore & Build**
```groovy
stage('Backend • Restore & Build') {
    steps {
        dir("${BACKEND_DIR}") {
            bat "dotnet restore"
            bat "dotnet build --configuration Release --no-restore"
            bat "dotnet publish --configuration Release --no-restore -o publish"
        }
    }
}
```

**Mục đích:**
- Restore các NuGet packages
- Build project .NET ở chế độ Release
- Publish ứng dụng vào thư mục `publish/`

**Lệnh thực thi:**
1. `dotnet restore` - Tải về các package từ NuGet
2. `dotnet build --configuration Release` - Build với cấu hình Release
3. `dotnet publish -o publish` - Xuất file executable vào thư mục publish

**Kết quả:**
- File `.dll` và dependencies trong `Backend/publish/`

---

#### **Stage 4: Frontend • Install & Build**
```groovy
stage('Frontend • Install & Build') {
    steps {
        dir("${FRONTEND_DIR}") {
            bat "npm ci"
            bat "npm run build -- --configuration production"
        }
    }
}
```

**Mục đích:**
- Cài đặt dependencies từ `package-lock.json` (đảm bảo version chính xác)
- Build Angular app ở chế độ production

**Lệnh thực thi:**
1. `npm ci` - Clean install (xóa node_modules và cài lại từ lock file)
2. `npm run build -- --configuration production` - Build Angular với optimization

**Kết quả:**
- File bundle trong `Frontend/dist/signalr-angular-frontend/`

---

#### **Stage 5: Docker • Build images**
```groovy
stage('Docker • Build images') {
    steps {
        dir("${BACKEND_DIR}") {
            bat "docker build -t ${BACKEND_IMAGE}:${IMAGE_TAG} ."
        }
        dir("${FRONTEND_DIR}") {
            bat "docker build -t ${FRONTEND_IMAGE}:${IMAGE_TAG} ."
        }
    }
}
```

**Mục đích:**
- Build Docker image cho Backend và Frontend
- Tag image với commit hash và build number

**Lệnh thực thi:**
1. `docker build -t signalr-backend-local:2fd8587-15 .` - Build backend image
2. `docker build -t signalr-frontend-local:2fd8587-15 .` - Build frontend image

**Kết quả:**
- 2 Docker images được tạo trong local Docker registry

---

#### **Stage 6: Deploy to server**
```groovy
stage('Deploy to server') {
    steps {
        bat """
            docker rm -f signalr-backend 2>NUL || echo ignore
            docker rm -f signalr-frontend 2>NUL || echo ignore
            docker run -d --restart always -p 8888:8080 --name signalr-backend ${BACKEND_IMAGE}:${IMAGE_TAG}
            docker run -d --restart always -p 9998:80 --name signalr-frontend ${FRONTEND_IMAGE}:${IMAGE_TAG}
        """
    }
}
```

**Mục đích:**
- Dừng và xóa container cũ (nếu có)
- Chạy container mới với image vừa build

**Lệnh thực thi:**
1. `docker rm -f signalr-backend` - Xóa container backend cũ
2. `docker rm -f signalr-frontend` - Xóa container frontend cũ
3. `docker run -d --restart always -p 8888:8080 ...` - Chạy backend container
   - `-d`: Chạy ở background (detached mode)
   - `--restart always`: Tự động restart khi container crash hoặc server reboot
   - `-p 8888:8080`: Map port 8888 (host) → 8080 (container)
   - `--name signalr-backend`: Đặt tên container
4. `docker run -d --restart always -p 9998:80 ...` - Chạy frontend container
   - `-p 9998:80`: Map port 9998 (host) → 80 (container, Nginx)

**Kết quả:**
- Backend chạy tại: `http://<Jenkins Server>:8888`
- Frontend chạy tại: `http://<Jenkins Server>:9998`
- SignalR Hub: `http://<Jenkins Server>:8888/notificationHub`

---

### Post Actions

```groovy
post {
    always {
        bat "docker image prune -f || echo ignore"  // Dọn dẹp image không dùng
    }
    success {
        echo "Build #${env.BUILD_NUMBER} (Windows) thành công"
    }
    failure {
        echo "Build #${env.BUILD_NUMBER} (Windows) thất bại"
    }
}
```

**Mục đích:**
- Luôn dọn dẹp Docker images không dùng (tiết kiệm dung lượng)
- Log thông báo kết quả build

---

## 🐧 Pipeline 2: Jenkinsfile.develop (Linux Agent - Develop Branch)

### Mục đích
Pipeline này được thiết kế để chạy trên **Linux Jenkins Agent** cho nhánh **`develop`**, build và deploy ứng dụng **cục bộ** (không push lên Docker Hub).

### Điểm khác biệt so với Jenkinsfile (main)

1. **Checkout từ nhánh `develop`** thay vì `main`
2. **Không có stage "Docker • Push images"** - chỉ build local
3. **Image tag**: `develop-<commit>-<build>` (ví dụ: `develop-2fd8587-15`)
4. **Deploy với ports khác**: Backend `8889`, Frontend `9999`
5. **Container names**: `signalr-backend-dev`, `signalr-frontend-dev`

### Chi tiết từng Stage

#### **Stage 1: Checkout**
```groovy
stage('Checkout') {
    steps {
        git branch: 'develop', url: 'https://github.com/ntdno1/LearnRabitMQ.git'
        script {
            env.GIT_COMMIT_SHORT = sh(script: "git rev-parse --short HEAD", returnStdout: true).trim()
            env.IMAGE_TAG = "develop-${GIT_COMMIT_SHORT}-${env.BUILD_NUMBER}"
        }
    }
}
```

**Mục đích:**
- Clone code từ GitHub nhánh `develop`
- Tạo image tag với prefix `develop-` để phân biệt với main

---

#### **Stage 2-5: Build Stages**
Tương tự như Jenkinsfile (main):
- Backend • Restore & Build
- Frontend • Install & Build
- Docker • Build images

**Khác biệt:** Không có stage "Docker • Push images"

---

#### **Stage 6: Deploy to server**
```groovy
stage('Deploy to server') {
    when {
        branch 'develop'
    }
    steps {
        script {
            echo "🚀 Deploying to Development environment..."
            echo "Backend port: 8889, Frontend port: 9999"
            
            sh """
                docker rm -f signalr-backend-dev || true
                docker rm -f signalr-frontend-dev || true
                docker run -d --restart always -p 8889:8080 --name signalr-backend-dev ${BACKEND_IMAGE}:${IMAGE_TAG}
                docker run -d --restart always -p 9999:80 --name signalr-frontend-dev ${FRONTEND_IMAGE}:${IMAGE_TAG}
            """
        }
    }
}
```

**Mục đích:**
- Deploy containers với ports khác với main để chạy song song
- Container names có suffix `-dev` để tránh conflict

**Kết quả:**
- Backend chạy tại: `http://<Jenkins Server>:8889`
- Frontend chạy tại: `http://<Jenkins Server>:9999`
- SignalR Hub: `http://<Jenkins Server>:8889/notificationHub`

---

## 🐧 Pipeline 3: Jenkinsfile (Linux Agent - Main Branch)

### Mục đích
Pipeline này được thiết kế để chạy trên **Linux Jenkins Agent**, có thêm bước **push Docker images lên Docker Hub**.

### Điểm khác biệt so với Jenkinsfile.windows

1. **Sử dụng `sh` thay vì `bat`** (Linux shell commands)
2. **Có stage "Docker • Push images"** để push lên Docker Hub
3. **Image names**: `ntdno1/signalr-backend` và `ntdno1/signalr-frontend` (thay vì `-local`)
4. **Deploy sử dụng `:latest` tag** (sau khi đã push)

### Chi tiết Stage bổ sung

#### **Stage: Docker • Push images**
```groovy
stage('Docker • Push images') {
    when {
        branch 'main'  // Chỉ push khi merge vào main
    }
    steps {
        withCredentials([usernamePassword(...)]) {
            sh """
                echo "${DOCKER_PASS}" | docker login -u "${DOCKER_USER}" --password-stdin
                docker tag ${BACKEND_IMAGE}:${IMAGE_TAG} ${BACKEND_IMAGE}:latest
                docker tag ${FRONTEND_IMAGE}:${IMAGE_TAG} ${FRONTEND_IMAGE}:latest
                docker push ${BACKEND_IMAGE}:${IMAGE_TAG}
                docker push ${BACKEND_IMAGE}:latest
                docker push ${FRONTEND_IMAGE}:${IMAGE_TAG}
                docker push ${FRONTEND_IMAGE}:latest
                docker logout
            """
        }
    }
}
```

**Mục đích:**
- Đăng nhập vào Docker Hub
- Tag image với `:latest` (bên cạnh tag có commit hash)
- Push cả 2 tags lên Docker Hub

**Lưu ý:**
- Cần tạo Jenkins credential với ID `docker-hub` chứa username/password Docker Hub
- Chỉ chạy khi branch là `main`

---

## 📦 Pipeline 4: SignalR_net_angular/Jenkinsfile

### Mục đích
Pipeline đơn giản nhất, chỉ build Docker images và deploy, không có bước build source code riêng (vì build được thực hiện trong Dockerfile).

### Cấu trúc Pipeline

```
Checkout → Build Backend Image → Build Frontend Image → Deploy
```

### Chi tiết từng Stage

#### **Stage 1: Checkout**
```groovy
stage('Checkout') {
    steps {
        git branch: 'main', url: 'https://github.com/ntdno1/LearnRabitMQ.git'
    }
}
```

**Đơn giản hơn:** Không tạo image tag, chỉ checkout code.

---

#### **Stage 2: Build Backend**
```groovy
stage('Build Backend') {
    steps {
        sh "docker build -t ${IMAGE_BACKEND}:latest -f SignalR_net_angular/Backend/Dockerfile SignalR_net_angular/Backend/"
    }
}
```

**Mục đích:**
- Build Docker image trực tiếp từ Dockerfile
- Dockerfile sẽ tự động:
  1. Restore packages
  2. Build project
  3. Publish
  4. Tạo runtime image

---

#### **Stage 3: Build Frontend**
```groovy
stage('Build Frontend') {
    steps {
        sh "docker build -t ${IMAGE_FRONTEND}:latest -f SignalR_net_angular/Frontend/Dockerfile SignalR_net_angular/Frontend/"
    }
}
```

**Mục đích:**
- Build Docker image cho Frontend
- Dockerfile sẽ:
  1. Cài npm packages
  2. Build Angular app
  3. Copy vào Nginx image

---

#### **Stage 4: Deploy**
```groovy
stage('Deploy') {
    when {
        branch 'main'  // Chỉ deploy khi merge vào main
    }
    steps {
        sh "docker rm -f ${CONTAINER_BACKEND} || true"
        sh "docker rm -f ${CONTAINER_FRONTEND} || true"
        sh "docker run -d --restart always -p ${PORT_BACKEND}:8080 --name ${CONTAINER_BACKEND} ${IMAGE_BACKEND}:latest"
        sh "docker run -d --restart always -p ${PORT_FRONTEND}:80 --name ${CONTAINER_FRONTEND} ${IMAGE_FRONTEND}:latest"
    }
}
```

**Mục đích:**
- Deploy containers với ports từ environment variables:
  - `PORT_BACKEND = "8888"`
  - `PORT_FRONTEND = "9998"`

---

## 🔄 Quy trình CI/CD hoàn chỉnh

### Bước 1: Developer push code lên GitHub

```bash
git add .
git commit -m "Update feature"
git push origin main
```

### Bước 2: GitHub gửi Webhook tới Jenkins

1. **GitHub** phát hiện có push vào nhánh `main`
2. **GitHub** gửi POST request tới Jenkins webhook endpoint:
   ```
   POST https://<jenkins-url>/github-webhook/
   ```
3. **Jenkins** nhận webhook và trigger pipeline

### Bước 3: Jenkins chạy Pipeline

1. **Checkout** code từ GitHub
2. **Build** Backend (.NET)
3. **Build** Frontend (Angular)
4. **Build** Docker images
5. **Push** images (nếu dùng Jenkinsfile root)
6. **Deploy** containers

### Bước 4: Kiểm tra kết quả

- **Backend**: `http://<server>:8888/swagger`
- **Frontend**: `http://<server>:9998`
- **SignalR Hub**: `http://<server>:8888/notificationHub`

---

## ⚙️ Cấu hình Jenkins Job

### Tạo Pipeline Job

1. **Vào Jenkins Dashboard** → **New Item**
2. **Chọn "Pipeline"** → Đặt tên (ví dụ: `SignalR-CICD`)
3. **Cấu hình:**

#### **General Settings:**
- ✅ **Discard old builds**: Giữ 15 builds gần nhất
- ✅ **Do not allow concurrent builds**

#### **Build Triggers:**
- ✅ **GitHub hook trigger for GITScm polling**

#### **Pipeline:**
- **Definition**: `Pipeline script from SCM`
- **SCM**: `Git`
- **Repository URL**: `https://github.com/ntdno1/LearnRabitMQ.git`
- **Branches to build**: 
  - `*/main` (cho production)
  - `*/develop` (cho development)
- **Script Path**: 
  - `Jenkinsfile.windows` (cho Windows agent, nhánh main)
  - `Jenkinsfile` (cho Linux agent, nhánh main)
  - `Jenkinsfile.develop` (cho Linux agent, nhánh develop)
  - `SignalR_net_angular/Jenkinsfile` (cho pipeline đơn giản)

### Cấu hình GitHub Webhook

1. **Vào GitHub Repository** → **Settings** → **Webhooks**
2. **Add webhook:**
   - **Payload URL**: `https://<jenkins-url>/github-webhook/`
   - **Content type**: `application/json`
   - **Events**: Chọn `Just the push event`
   - **Active**: ✅

### Cấu hình Docker Hub Credentials (nếu dùng Jenkinsfile root)

1. **Jenkins** → **Manage Jenkins** → **Manage Credentials**
2. **Add Credentials:**
   - **Kind**: `Username with password`
   - **Username**: Docker Hub username
   - **Password**: Docker Hub password/token
   - **ID**: `docker-hub` (phải đúng với `DOCKERHUB_CREDENTIALS` trong Jenkinsfile)

---

## 🐛 Troubleshooting

### Lỗi: "Could not find credentials entry with ID 'docker-hub'"

**Nguyên nhân:** Jenkins không tìm thấy credential Docker Hub.

**Giải pháp:**
1. Tạo credential với ID chính xác là `docker-hub`
2. Hoặc sửa `DOCKERHUB_CREDENTIALS` trong Jenkinsfile thành ID credential bạn đã tạo

---

### Lỗi: "Invalid option type 'ansiColor'"

**Nguyên nhân:** Plugin AnsiColor chưa được cài đặt.

**Giải pháp:**
1. **Jenkins** → **Manage Jenkins** → **Manage Plugins**
2. **Available** → Tìm "AnsiColor" → **Install**
3. Hoặc xóa dòng `ansiColor('xterm')` trong Jenkinsfile (đã được comment trong code hiện tại)

---

### Lỗi: "failed to read dockerfile: open Dockerfile: no such file or directory"

**Nguyên nhân:** `.dockerignore` đang chặn Dockerfile.

**Giải pháp:**
- Kiểm tra file `.dockerignore` trong thư mục Frontend/Backend
- Đảm bảo không có dòng `Dockerfile` hoặc `nginx.conf` trong `.dockerignore`

---

### Lỗi: "Skipped Pipeline script from SCM because it doesn't have a matching repository"

**Nguyên nhân:** Job không có SCM Git được cấu hình đúng.

**Giải pháp:**
1. Vào **Configure** job
2. **Pipeline** → **Definition**: Chọn `Pipeline script from SCM`
3. **SCM**: Chọn `Git` và điền đúng Repository URL
4. **Script Path**: Điền đúng đường dẫn Jenkinsfile (ví dụ: `Jenkinsfile.windows`)

---

### Frontend không kết nối được Backend

**Nguyên nhân:** URL SignalR Hub trong code Frontend không đúng.

**Giải pháp:**
1. Kiểm tra file `Frontend/src/app/services/signalr.service.ts`
2. Đảm bảo URL là: `http://localhost:8888/notificationHub` (hoặc địa chỉ server thực tế)
3. **Quan trọng:** Code trong container được build từ code trên GitHub, không phải code local
4. Commit và push thay đổi lên GitHub, sau đó rebuild container

---

## 📊 Monitoring và Logs

### Xem Build Logs

1. **Vào Jenkins Job** → **Build History**
2. **Click vào build number** → **Console Output**
3. Xem chi tiết từng stage đã chạy

### Kiểm tra Containers

```bash
# Xem danh sách containers đang chạy
docker ps

# Xem logs của backend container
docker logs signalr-backend

# Xem logs của frontend container
docker logs signalr-frontend
```

### Kiểm tra Ports

```bash
# Windows
netstat -ano | findstr :8888
netstat -ano | findstr :9998

# Linux
netstat -tulpn | grep :8888
netstat -tulpn | grep :9998
```

---

## 🎯 Best Practices

### 1. Sử dụng Image Tags có ý nghĩa
- Tag với commit hash: `2fd8587-15` (dễ trace về commit)
- Tag `latest` cho production

### 2. Cleanup Docker Images
- Luôn có `docker image prune` trong post actions
- Tránh đầy ổ cứng

### 3. Restart Policy
- Sử dụng `--restart always` để container tự động restart khi crash

### 4. Environment Variables
- Sử dụng biến môi trường cho ports, image names
- Dễ thay đổi cấu hình mà không cần sửa code

### 5. Separate Build và Deploy
- Build images trước, deploy sau
- Dễ rollback nếu deploy lỗi

---

## 📝 Tóm tắt Ports

### Nhánh `main` (Production)

| Service | Container Port | Host Port | URL | Container Name | Jenkinsfile |
|---------|---------------|-----------|-----|----------------|------------|
| Backend | 8080 | 8888 | `http://<server>:8888` | `signalr-backend` | `Jenkinsfile` |
| Frontend | 80 | 9998 | `http://<server>:9998` | `signalr-frontend` | `Jenkinsfile` |
| SignalR Hub | 8080 | 8888 | `http://<server>:8888/notificationHub` | - | - |
| Swagger | 8080 | 8888 | `http://<server>:8888/swagger` | - | - |

### Nhánh `develop` (Development)

| Service | Container Port | Host Port | URL | Container Name | Jenkinsfile |
|---------|---------------|-----------|-----|----------------|------------|
| Backend | 8080 | 8889 | `http://<server>:8889` | `signalr-backend-dev` | `Jenkinsfile.develop` |
| Frontend | 80 | 9999 | `http://<server>:9999` | `signalr-frontend-dev` | `Jenkinsfile.develop` |
| SignalR Hub | 8080 | 8889 | `http://<server>:8889/notificationHub` | - | - |
| Swagger | 8080 | 8889 | `http://<server>:8889/swagger` | - | - |

**Lưu ý:** 
- Ports khác nhau giữa `main` và `develop` để có thể chạy song song cả 2 môi trường trên cùng một server
- Nhánh `develop` sử dụng `Jenkinsfile.develop` riêng, không push Docker Hub

---

## 🔗 Tài liệu tham khảo

- [Jenkins Pipeline Documentation](https://www.jenkins.io/doc/book/pipeline/)
- [Docker Documentation](https://docs.docker.com/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Angular Deployment](https://angular.io/guide/deployment)

---

**Chúc bạn triển khai CI/CD thành công! 🚀**

