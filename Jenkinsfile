pipeline {
    agent any

    options {
        skipDefaultCheckout(true)
        // ansiColor('xterm')
        timestamps()
        buildDiscarder(logRotator(numToKeepStr: '15'))
        disableConcurrentBuilds()
    }

    environment {
        BACKEND_DIR = "SignalR_net_angular/Backend"
        FRONTEND_DIR = "SignalR_net_angular/Frontend"
        BACKEND_IMAGE = "ntdno1/signalr-backend"
        FRONTEND_IMAGE = "ntdno1/signalr-frontend"
        DOCKERHUB_CREDENTIALS = "docker-hub" // Jenkins credential ID
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', url: 'https://github.com/ntdno1/LearnRabitMQ.git'
                script {
                    env.GIT_COMMIT_SHORT = sh(script: "git rev-parse --short HEAD", returnStdout: true).trim()
                    env.IMAGE_TAG = "${GIT_COMMIT_SHORT}-${env.BUILD_NUMBER}"
                }
            }
        }

        stage('Verify merge target') {
            when {
                changeRequest(target: 'main')
            }
            steps {
                echo "Change Request nhắm tới nhánh main - bật toàn bộ bước CI cần thiết."
            }
        }

        stage('Backend • Restore & Build') {
            steps {
                dir("${BACKEND_DIR}") {
                    sh "dotnet restore"
                    sh "dotnet build --configuration Release --no-restore"
                    sh "dotnet publish --configuration Release --no-restore -o publish"
                }
            }
        }

        stage('Frontend • Install & Build') {
            steps {
                dir("${FRONTEND_DIR}") {
                    sh "npm ci"
                    sh "npm run build -- --configuration production"
                }
            }
        }

        stage('Docker • Build images') {
            steps {
                sh """
                    docker build -t ${BACKEND_IMAGE}:${IMAGE_TAG} -f ${BACKEND_DIR}/Dockerfile ${BACKEND_DIR}
                    docker build -t ${FRONTEND_IMAGE}:${IMAGE_TAG} -f ${FRONTEND_DIR}/Dockerfile ${FRONTEND_DIR}
                """
            }
        }

        stage('Docker • Push images') {
            when {
                branch 'main'
            }
            steps {
                withCredentials([usernamePassword(credentialsId: "${DOCKERHUB_CREDENTIALS}", usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
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

        stage('Deploy to server') {
            when {
                allOf {
                    branch 'main'
                    expression { return env.DEPLOY_ENABLED?.toBoolean() ?: true }
                }
            }
            steps {
                sh """
                    docker rm -f signalr-backend || true
                    docker rm -f signalr-frontend || true
                    docker run -d --restart always -p 8888:8080 --name signalr-backend ${BACKEND_IMAGE}:latest
                    docker run -d --restart always -p 9998:80 --name signalr-frontend ${FRONTEND_IMAGE}:latest
                """
            }
        }
    }

    post {
        always {
            sh "docker image prune -f || true"
        }
        success {
            echo "Build #${env.BUILD_NUMBER} thành công"
        }
        failure {
            echo "Build #${env.BUILD_NUMBER} thất bại"
        }
    }
}