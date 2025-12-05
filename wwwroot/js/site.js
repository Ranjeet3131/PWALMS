// Quiz Timer
function startQuizTimer(minutes, attemptId) {
    let time = minutes * 60;
    const timerElement = document.getElementById('quizTimer');

    const timer = setInterval(function () {
        const minutesLeft = Math.floor(time / 60);
        let secondsLeft = time % 60;

        secondsLeft = secondsLeft < 10 ? '0' + secondsLeft : secondsLeft;
        timerElement.textContent = `${minutesLeft}:${secondsLeft}`;

        if (time <= 60) {
            timerElement.classList.add('text-danger');
        } else if (time <= 300) {
            timerElement.classList.add('text-warning');
        }

        if (time <= 0) {
            clearInterval(timer);
            timerElement.textContent = 'TIME UP!';
            alert('Time is up! Submitting your quiz...');
            submitQuiz(attemptId);
        }

        time--;
    }, 1000);
}

// Auto-dismiss alerts
setTimeout(function () {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        const bsAlert = new bootstrap.Alert(alert);
        bsAlert.close();
    });
}, 5000);