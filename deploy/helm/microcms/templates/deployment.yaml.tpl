apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "microcms.fullname" . }}
  labels:
    {{- include "microcms.labels" . | nindent 4 }}
spec:
  {{- if not .Values.autoscaling.enabled }}
  replicas: {{ .Values.replicaCount }}
  {{- end }}
  selector:
    matchLabels:
      {{- include "microcms.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      annotations:
        {{- with .Values.podAnnotations }}
        {{- toYaml . | nindent 8 }}
        {{- end }}
      labels:
        {{- include "microcms.labels" . | nindent 8 }}
        {{- with .Values.podLabels }}
        {{- toYaml . | nindent 8 }}
        {{- end }}
    spec:
      {{- with .Values.imagePullSecrets }}
      imagePullSecrets:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      serviceAccountName: {{ include "microcms.serviceAccountName" . }}
      securityContext:
        {{- toYaml .Values.podSecurityContext | nindent 8 }}
      containers:
        - name: webhost
          securityContext:
            {{- toYaml .Values.containerSecurityContext | nindent 12 }}
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag | default .Chart.AppVersion }}"
          imagePullPolicy: {{ .Values.image.pullPolicy }}
          ports:
            - name: http
              containerPort: 8080
              protocol: TCP
          env:
            {{- range $key, $value := .Values.env }}
            - name: {{ $key }}
              value: {{ $value | quote }}
            {{- end }}
            {{- if .Values.existingSecret }}
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: {{ .Values.existingSecret }}
                  key: db-connection-string
            - name: MicroCMS__Cache__ConnectionString
              valueFrom:
                secretKeyRef:
                  name: {{ .Values.existingSecret }}
                  key: redis-connection-string
            - name: Telemetry__OtlpEndpoint
              valueFrom:
                secretKeyRef:
                  name: {{ .Values.existingSecret }}
                  key: otlp-endpoint
                  optional: true
            {{- end }}
          livenessProbe:
            {{- toYaml .Values.livenessProbe | nindent 12 }}
          readinessProbe:
            {{- toYaml .Values.readinessProbe | nindent 12 }}
          resources:
            {{- toYaml .Values.resources | nindent 12 }}
          {{- if .Values.persistence.logs.enabled }}
          volumeMounts:
            - name: logs
              mountPath: /app/App_Data/logs
          {{- end }}
      {{- if .Values.persistence.logs.enabled }}
      volumes:
        - name: logs
          persistentVolumeClaim:
            claimName: {{ include "microcms.fullname" . }}-logs
      {{- end }}
      {{- with .Values.nodeSelector }}
      nodeSelector:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.affinity }}
      affinity:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.tolerations }}
      tolerations:
        {{- toYaml . | nindent 8 }}
      {{- end }}
